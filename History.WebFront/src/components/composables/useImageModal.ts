
import { ref } from 'vue';
import type { MediaContent } from '@/types';

export function useImageModal(mediaUrlMap: Record<string, string>) {
  const showImageModal = ref(false);
  type MediaSourceItem = { src: string; type: string; mimeType?: string; };

  const modalMediaSource = ref<MediaSourceItem[]>([]);
  const initialSlideIndex = ref(0);

  const openImageModal = (mediaList: MediaContent[], index: number) => {
    modalMediaSource.value = mediaList.map(content => {
      let src = '';
      let type = 'image';
      const mimeType = content.mimeType || '';

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
