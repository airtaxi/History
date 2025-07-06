import { ref, watch, onUnmounted } from 'vue';

export function useFileAttachment() {
  const attachedFiles = ref<File[]>([]);
  const previewItems = ref<{ url: string; isVideo: boolean; file: File }[]>([]);
  const isDragOver = ref(false);

  // 파일 추가 함수
  const addFiles = (files: FileList | File[]) => {
    const newFiles = Array.from(files);
    attachedFiles.value = [...attachedFiles.value, ...newFiles];
    updatePreviewItems();
  };

  // 특정 파일 제거 함수
  const removeFile = (index: number) => {
    if (index >= 0 && index < attachedFiles.value.length) {
      const removedFile = attachedFiles.value[index];
      attachedFiles.value.splice(index, 1);
      // 미리보기 URL 해제
      const previewItem = previewItems.value.find(item => item.file === removedFile);
      if (previewItem) {
        URL.revokeObjectURL(previewItem.url);
      }
      updatePreviewItems();
    }
  };

  // 미리보기 아이템 업데이트
  const updatePreviewItems = () => {
    // 기존 미리보기 URL 해제
    previewItems.value.forEach(item => URL.revokeObjectURL(item.url));

    previewItems.value = attachedFiles.value.map(file => ({
      url: URL.createObjectURL(file),
      isVideo: file.type.startsWith('video/'),
      file: file, // 원본 파일 참조를 추가하여 제거 시 활용
    }));
  };

  // 클립보드 붙여넣기 처리
  const handlePaste = (event: ClipboardEvent) => {
    const items = event.clipboardData?.items;
    if (items) {
      const filesToAttach: File[] = [];
      for (let i = 0; i < items.length; i++) {
        if (items[i].type.indexOf('image') !== -1) {
          const blob = items[i].getAsFile();
          if (blob) {
            // Blob을 File 객체로 변환 (이름 부여)
            const file = new File([blob], `pasted_image_${Date.now()}.png`, { type: blob.type });
            filesToAttach.push(file);
          }
        }
      }
      if (filesToAttach.length > 0) {
        event.preventDefault(); // 기본 붙여넣기 동작 방지
        addFiles(filesToAttach);
      }
    }
  };

  // 드래그 앤 드롭 처리
  const handleDrop = (event: DragEvent) => {
    event.preventDefault();
    isDragOver.value = false;
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      addFiles(files);
    }
  };

  const handleDragOver = (event: DragEvent) => {
    event.preventDefault();
    isDragOver.value = true;
  };

  const handleDragLeave = (event: DragEvent) => {
    event.preventDefault();
    isDragOver.value = false;
  };

  // DOM 요소에 드래그 앤 드롭 이벤트 리스너 설정
  const setupDragAndDrop = (element: HTMLElement) => {
    if (element) {
      element.addEventListener('dragover', handleDragOver);
      element.addEventListener('dragleave', handleDragLeave);
      element.addEventListener('drop', handleDrop);
    }
  };

  // 컴포넌트 언마운트 시 URL 해제
  onUnmounted(() => {
    previewItems.value.forEach(item => URL.revokeObjectURL(item.url));
  });

  // attachedFiles 변경 감지 (외부에서 직접 변경될 경우 대비)
  watch(attachedFiles, () => {
    updatePreviewItems();
  }, { deep: true });

  return {
    attachedFiles,
    previewItems,
    isDragOver,
    addFiles,
    removeFile,
    handlePaste,
    handleDrop,
    setupDragAndDrop,
  };
}
