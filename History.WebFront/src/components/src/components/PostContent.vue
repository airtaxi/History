<!--
 * PostContent.vue
 *
 * 이 컴포넌트는 게시물의 본문 콘텐츠(텍스트, 미디어, 외부 링크)를 렌더링합니다.
 * Swiper 라이브러리를 사용하여 미디어 슬라이더를 구현하고,
 * 텍스트 콘텐츠 내의 링크 및 @멘션을 파싱하여 적절하게 표시합니다.
 *
 * @props {
 *   contents: Array<any> - 게시물 콘텐츠 배열 (텍스트, 미디어, 외부 링크 등).
 *   mediaUrlMap: Record<string, string> - 미디어 ID와 Blob URL 매핑 객체.
 * }
 * @emits {
 *   open-media-modal: 미디어(이미지/비디오) 클릭 시 전체 화면 모달을 열기 위해 발생.
 *   navigate-to-profile: @멘션 클릭 시 해당 사용자 프로필로 이동하기 위해 발생.
 * }
-->
<template>
  <div class="post-content-area">
    <div v-for="(content, index) in processedContents" :key="index">
      <template v-if="content.$type === 'mediaGroup'">
        <Swiper
          ref="mediaSwiperRef"
          class="media-swiper"
          :spaceBetween="10"
          :slidesPerView="1"
          :loop="content.media.length > 1"
          :navigation="true"
          :pagination="{ clickable: true }"
          :modules="modules"
        >
          <SwiperSlide v-for="(mediaItem, mediaIndex) in content.media" :key="mediaIndex">
            <div v-if="mediaUrlMap[mediaItem.mediaId || mediaItem.thumbnailMediaId]">
              <video
                v-if="mediaItem.mimeType?.startsWith('video/')"
                controls
                class="post-vidio"
                @click.stop="openImageModal(content.media, mediaIndex)"
              >
                <source :src="mediaUrlMap[mediaItem.mediaId || mediaItem.thumbnailMediaId]" :type="mediaItem.mimeType" />
              </video>
              <img
                v-else
                :src="mediaUrlMap[mediaItem.mediaId || mediaItem.thumbnailMediaId]"
                alt="게시물 이미지"
                class="post-image"
                @click.stop="openImageModal(content.media, mediaIndex)"
              />
            </div>
          </SwiperSlide>
        </Swiper>
      </template>
      <template v-else-if="content.$type === 'text'">
        <PostText :text="content.text" @navigate-to-profile="$emit('navigate-to-profile', $event)" />
      </template>
      <template v-else-if="content.$type === 'externalUrl'">
        <PostExternalLink :content="content" />
      </template>
      <RouterLink v-else-if="content.$type === 'profile'" :to="`/user/${content.userId}`" class="mention" @click.stop>{{ content.nickname }}</RouterLink>
      <div v-else-if="content.$type === 'UploadContent'"><p style="color: red;">[이미지 처리 실패] {{ content.FileName }}</p></div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { defineProps, defineEmits, ref, onMounted, nextTick, computed } from 'vue';
import { Swiper, SwiperSlide } from 'swiper/vue';
import { Navigation, Pagination } from 'swiper/modules';
import 'swiper/css';
import 'swiper/css/navigation';
import 'swiper/css/pagination';

import PostText from './PostText.vue';
import PostExternalLink from './PostExternalLink.vue';

const props = defineProps({
  contents: {
    type: Array as () => Array<any>,
    required: true,
  },
  mediaUrlMap: {
    type: Object as () => Record<string, string>,
    required: true,
  },
});

const emit = defineEmits(['open-media-modal', 'navigate-to-profile']);

const modules = [Navigation, Pagination];

const mediaSwiperRef = ref(null); // Swiper 인스턴스를 참조할 ref 추가

// Swiper 네비게이션 버튼의 클릭 이벤트 전파를 막는 헬퍼 함수
  const addStopPropagationToSwiperNav = (swiperInstanceRef: any) => {
    if (!swiperInstanceRef) return;

    const swiperEl = swiperInstanceRef.$el;
    if (!swiperEl) return;

    const nextBtn = swiperEl.querySelector('.swiper-button-next');
    const prevBtn = swiperEl.querySelector('.swiper-button-prev');

    if (nextBtn) {
      nextBtn.addEventListener('click', (e: Event) => e.stopPropagation());
    }
    if (prevBtn) {
      prevBtn.addEventListener('click', (e: Event) => e.stopPropagation());
    }
  };

onMounted(async () => {
  // Vue가 DOM 업데이트를 완료한 후 Swiper 로직을 실행하도록 보장합니다.
  await nextTick();
  addStopPropagationToSwiperNav(mediaSwiperRef.value);
});

const processedContents = computed(() => {
  const hasMedia = props.contents.some(content => content.$type === 'media');

  let sourceContents = props.contents;
  if (hasMedia) {
    sourceContents = props.contents.filter(content => content.$type !== 'externalUrl');
  }

  const result: Array<any> = [];
  let mediaGroup: Array<any> = [];

  sourceContents.forEach(content => {
    if (content.$type === 'media') {
      mediaGroup.push(content);
    } else {
      if (mediaGroup.length > 0) {
        result.push({ $type: 'mediaGroup', media: mediaGroup });
        mediaGroup = [];
      }
      result.push(content);
    }
  });

  if (mediaGroup.length > 0) {
    result.push({ $type: 'mediaGroup', media: mediaGroup });
  }
  return result;
});

const openImageModal = (mediaList: any[], index: number) => {
  emit('open-media-modal', mediaList, index);
};
</script>

<style scoped>
  /* 게시물 콘텐츠 영역 */
  .post-content-area {
    word-break: break-word;
  }

  /* 미디어 스와이퍼 */
  .media-swiper {
    width: 100%;
    height: auto;
  }

  /* 게시물 이미지 및 비디오 공통 스타일 */
  .post-image,
  .post-vidio {
    width: 100%;
    max-width: 100%;
    min-width: 300px;
    background-color: black;
    object-fit: cover;
    border-radius: 8px;
    display: block;
    margin: 12px auto;
  }

  .post-image {
    aspect-ratio:1;
  }

  .post-vidio {
    object-fit: scale-down;
    height: 100%;
  }

  /* Swiper 네비게이션 커스텀 */
  :deep(.swiper-button-next),
  :deep(.swiper-button-prev) {
    color: #ed664d;
  }

  :deep(.swiper-pagination) {
    bottom: -5px;
  }

  :deep(.swiper-pagination-bullet) {
    background-color: #A9A9A9;
    opacity: 0.8;
  }

  :deep(.swiper-pagination-bullet-active) {
    background-color: #ed664d;
  }
</style>
