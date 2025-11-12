import { ref } from 'vue';
import type { MediaContent, ExternalUrlContent } from '@/types';

export function useImageModal(mediaUrlMap: Record<string, string>) {
  const showImageModal = ref(false);
  const modalMediaSource = ref<any[]>([]);
  const initialSlideIndex = ref(0);

  const openImageModal = (mediaList: (MediaContent | ExternalUrlContent)[], index: number) => {
    modalMediaSource.value = mediaList.map(content => {
      let src = '';
      let type = 'image';
      const mimeType = (content as MediaContent).mimeType || '';

      if (content.$type === 'external' || content.$type === 'externalUrl') {
        src = (content as ExternalUrlContent).url || '';
      } else {
        const mediaId =
          (content as MediaContent).mediaId ||
          (content as MediaContent).thumbnailMediaId;
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
