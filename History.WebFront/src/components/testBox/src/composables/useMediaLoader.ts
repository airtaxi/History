/**
 * useMediaLoader
 *
 * 이 컴포저블은 미디어 파일(이미지, 비디오, 프로필 사진 등)을 효율적으로 로드하고 캐싱하는 기능을 제공합니다.
 * 서버에서 미디어 ID를 통해 파일을 가져와 Blob URL로 변환하고, 이를 캐시하여 중복 요청을 방지합니다.
 *
 * @returns {Object} 미디어 로딩 관련 상태 및 함수들을 포함하는 객체
 * @property {Ref<Record<string, string>>} mediaUrlMap - 게시물 미디어(이미지/비디오)의 Blob URL 캐시.
 * @property {Ref<Record<string, string>>} profileBlobUrlMap - 사용자 프로필 이미지의 Blob URL 캐시.
 * @property {Function} getMediaBlobUrl - Media ID를 받아 해당 미디어의 Blob URL을 반환하는 함수.
 */
import { ref, type Ref } from 'vue';
import apiClient from '@/api'; // apiClient 임포트

export function useMediaLoader() {
  // 게시물 미디어(이미지/비디오)의 Blob URL을 캐싱하는 맵
  const mediaUrlMap: Ref<Record<string, string>> = ref({});
  // 사용자 프로필 이미지의 Blob URL을 캐싱하는 맵
  const profileBlobUrlMap: Ref<Record<string, string>> = ref({});

  /**
   * 주어진 mediaId에 해당하는 미디어 파일의 Blob URL을 가져옵니다.
   * 이미 캐시된 경우 캐시된 URL을 반환하고, 그렇지 않으면 서버에서 가져와 캐싱합니다.
   * @param {string} mediaId - 가져올 미디어 파일의 ID.
   * @returns {Promise<string>} 미디어 파일의 Blob URL.
   */
  const getMediaBlobUrl = async (mediaId: string): Promise<string> => {
    if (mediaUrlMap.value[mediaId]) {
      return mediaUrlMap.value[mediaId];
    }

    try {
      const response = await apiClient.get(`/api/media/${mediaId}`, { responseType: 'blob' });
      const blob = response.data;
      const blobUrl = URL.createObjectURL(blob);
      mediaUrlMap.value[mediaId] = blobUrl;
      return blobUrl;
    } catch (error) {
      console.error(`Error loading media ${mediaId}:`, error);
      // 에러 발생 시 기본 이미지 또는 빈 문자열 반환
      return '';
    }
  };

  return {
    mediaUrlMap,
    profileBlobUrlMap,
    getMediaBlobUrl,
  };
}
