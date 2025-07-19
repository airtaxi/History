
import { ref } from 'vue';

export function useImageModal(mediaUrlMap: Record<string, string>) {
  const showImageModal = ref(false);
  const modalMediaSource = ref<any[]>([]);
  const initialSlideIndex = ref(0);

  const openImageModal = (mediaList: any[], index: number) => {
    modalMediaSource.value = mediaList.map(content => {
      let src = '';
      let type = 'image';
      let mimeType = content.mimeType || '';

      if (content.isExternal) {
        src = content.mediaId;
      } else {
        const mediaId = content.mediaId || content.thumbnailMediaId;
        src = mediaUrlMap[mediaId];
      }
      if (mimeType.startsWith('video/')) {
        type = 'video';
      }
      return { src, type, mimeType };
    });

    initialSlideIndex.value = index;
    showImageModal.value = true;
  };

  const closeImageModal = () => {
    showImageModal.value = false;
  };

  return {
    showImageModal,
    modalMediaSource,
    initialSlideIndex,
    openImageModal,
    closeImageModal,
  };
}
