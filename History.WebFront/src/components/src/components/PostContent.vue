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
    <!-- 미디어 슬라이더 (Swiper) -->
    <Swiper
      v-if="contents?.some(c => c.$type === 'media')"
      ref="mediaSwiperRef"
      class="media-swiper"
      :spaceBetween="10"
      :slidesPerView="1"
      :loop="contents?.filter(c => c.$type === 'media').length > 1"
      :navigation="true"
      :pagination="{ clickable: true }"
      :modules="modules"
    >
      <SwiperSlide v-for="(content, index) in contents?.filter(c => c.$type === 'media')" :key="index">
        <div v-if="mediaUrlMap[content.mediaId || content.thumbnailMediaId]">
          <video
            v-if="content.mimeType?.startsWith('video/')"
            controls
            class="post-image"
            @click.stop="$emit('open-media-modal', contents.filter(c => c.$type === 'media'), index)"
          >
            <source :src="mediaUrlMap[content.mediaId || content.thumbnailMediaId]" :type="content.mimeType" />
          </video>
          <img
            v-else
            :src="mediaUrlMap[content.mediaId || content.thumbnailMediaId]"
            alt="게시물 이미지"
            class="post-image"
            @click.stop="$emit('open-media-modal', contents.filter(c => c.$type === 'media'), index)"
          />
        </div>
      </SwiperSlide>
    </Swiper>

    <!-- 텍스트 및 외부 링크 콘텐츠 -->
    <div v-for="(content, index) in contents" :key="'extra-' + index">
      <template v-if="content.$type !== 'media'">
        <p v-if="content.$type === 'text'" style="white-space: pre-wrap; word-break: break-word;">
          <template v-for="(chunk, i) in splitTextWithLinksAndMentions(content.text)" :key="i">
            <a
              v-if="chunk.type === 'link'"
              :href="chunk.text.startsWith('www.') ? 'https://' + chunk.text : chunk.text"
              target="_blank"
              rel="noopener noreferrer"
              style="color: #0066cc; word-break: break-all;"
              @click.stop
            >{{ chunk.text }}</a>
            <span v-else-if="chunk.type === 'mention'" class="mention" @click.stop="$emit('navigate-to-profile', chunk.text)">{{ chunk.text }}</span>
            <span v-else>{{ chunk.text }}</span>
          </template>
        </p>
        <div v-else-if="content.$type === 'externalUrl'" class="external-link-container">
          <a
            :href="content.sourceUrl || content.SourceUrl || content.url || content.Url"
            target="_blank"
            rel="noopener noreferrer"
            class="external-link"
            @click.stop
          >
            <div class="link-preview" :class="{ 'has-image': !!content.thumbnailImageUrl || !!content.ThumbnailImageUrl || !!content.image || !!content.Image }">
              <img
                v-if="content.thumbnailImageUrl || content.ThumbnailImageUrl || content.image || content.Image"
                :src="content.thumbnailImageUrl || content.ThumbnailImageUrl || content.image || content.Image"
                :alt="content.title || content.Title || '링크 미리보기'"
                class="link-preview-image"
                @error="(e) => (e.target as HTMLImageElement).style.display = 'none'"
              />
              <div class="link-info">
                <div v-if="content.title || content.Title" class="link-title">{{ content.title || content.Title }}</div>
                <div v-if="content.description || content.Description" class="link-description">{{ content.description || content.Description }}</div>
                <div class="link-url"><span class="link-icon">🔗</span><span class="link-text">{{ content.sourceUrl || content.SourceUrl || content.url || content.Url }}</span></div>
              </div>
            </div>
          </a>
        </div>
        <RouterLink v-else-if="content.$type === 'profile'" :to="`/user/${content.userId}`" class="mention" @click.stop>{{ content.nickname }}</RouterLink>
        <div v-else-if="content.$type === 'UploadContent'"><p style="color: red;">[이미지 처리 실패] {{ content.FileName }}</p></div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { defineProps, defineEmits, ref, onMounted, nextTick } from 'vue';
import { Swiper, SwiperSlide } from 'swiper/vue';
import { Navigation, Pagination } from 'swiper/modules';
import 'swiper/css';
import 'swiper/css/navigation';
import 'swiper/css/pagination';

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

  // $el을 통해 실제 DOM 요소에 접근합니다.
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

/**
 * 텍스트에서 @멘션과 링크를 감지하여 분리합니다.
 * @param {string} text - 원본 텍스트.
 * @returns {Array<{ text: string; type: 'text' | 'link' | 'mention' }>} 분리된 텍스트 청크 배열.
 */
function splitTextWithLinksAndMentions(text: string): Array<{ text: string; type: 'text' | 'link' | 'mention' }> {
  const urlRegex = /(?:https?:\/\/[^\s]+)|(?:www\.[^\s]+)|(?:[a-zA-Z0-9][a-zA-Z0-9-]*(?:\.[a-zA-Z0-9][a-zA-Z0-9-]*)+(?:\/[^\s]*)?)/g;
  const mentionRegex = /@[a-zA-Z0-9_가-힣\s]+/g;

  const matches: Array<{ text: string; type: 'link' | 'mention'; index: number; length: number }> = [];

  let match;
  while ((match = urlRegex.exec(text)) !== null) {
    matches.push({ text: match[0], type: 'link', index: match.index, length: match[0].length });
  }

  while ((match = mentionRegex.exec(text)) !== null) {
    matches.push({ text: match[0], type: 'mention', index: match.index, length: match[0].length });
  }

  matches.sort((a, b) => a.index - b.index);

  const result: Array<{ text: string; type: 'text' | 'link' | 'mention' }> = [];
  let lastIndex = 0;

  for (const match of matches) {
    if (match.index > lastIndex) {
      result.push({ text: text.slice(lastIndex, match.index), type: 'text' });
    }
    result.push({ text: match.text, type: match.type });
    lastIndex = match.index + match.length;
  }

  if (lastIndex < text.length) {
    result.push({ text: text.slice(lastIndex), type: 'text' });
  }

  return result;
}

/**
 * URL이 이미지 URL인지 판단하는 함수.
 * @param {string} url - 검사할 URL.
 * @returns {boolean} 이미지 URL인지 여부.
 */
const isImageUrl = (url: string): boolean => {
  if (!url || typeof url !== 'string') return false;
  const trimmedUrl = url.trim();
  const imageExtensions = /\.(jpg|jpeg|png|gif|webp|bmp|svg|ico|avif|tiff|tif)(\?.*)?$/i;
  if (imageExtensions.test(trimmedUrl)) return true;

  const imageServices = [
    'dribbble.com', 'imgur.com', 'cloudinary.com', 'unsplash.com', 'pexels.com',
    'instagram.com', 'pinimg.com', 'googleusercontent.com', 'githubusercontent.com',
    'flickr.com', 'staticflickr.com', 'photobucket.com', 'imageshack.com',
    'tinypic.com', 'deviantart.net', 'twimg.com', 'discordapp.com', 'discord.com',
    'ibb.co', 'imgbb.com', 'i.imgur.com', 'prnt.sc', 'gyazo.com'
  ];
  const lowerUrl = trimmedUrl.toLowerCase();
  if (imageServices.some(service => lowerUrl.includes(service))) return true;

  const imageKeywords = ['/image/', '/img/', '/photo/', '/picture/', '/media/', '/upload/', '/file/original'];
  if (imageKeywords.some(keyword => lowerUrl.includes(keyword))) return true;

  if (!trimmedUrl.startsWith('http://') && !trimmedUrl.startsWith('https://')) return false;

  return false;
};
</script>

<style scoped>
.post-content-area {
  margin-bottom: 12px;
  word-break: break-word;
}

.media-swiper {
  width: 100%;
  height: auto;
}

.post-image {
  width: 100%;
  max-width: 100%;
  min-width: 300px;
  height: 500px;
  background-color: black;
  object-fit: cover;
  border-radius: 8px;
  display: block;
  margin: 12px auto;
}

.external-link-container {
  display: flex;
  justify-content: center;
}

.external-link {
  text-decoration: none;
  color: inherit;
  display: block;
  max-width: 100%;
  width: 100%;
}

.link-preview {
  border: 1px solid #e1e5e9;
  border-radius: 12px;
  background: white;
  transition: all 0.2s ease;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

.link-preview:hover {
  border-color: #d1d5db;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
}

/* 이미지가 있는 링크 미리보기 레이아웃 */
.link-preview.has-image {
  display: flex;
}

/* 큰 이미지 (일반 포스트) */
.link-preview.has-image:not(.small) {
  flex-direction: column;
}
.link-preview.has-image:not(.small) .link-preview-image {
  width: 100%;
  height: 200px;
  object-fit: cover;
}
.link-preview.has-image:not(.small) .link-info {
  padding: 16px;
}

/* 작은 이미지 (리포스트 안의 원본글) */
.link-preview.small.has-image {
  flex-direction: row;
  align-items: stretch;
}
.link-preview.small.has-image .link-preview-image {
  width: 100px;
  height: auto;
  object-fit: cover;
  flex-shrink: 0;
  border-right: 1px solid #e1e5e9;
}
.link-preview.small.has-image .link-info {
  padding: 12px;
}

/* 이미지가 없는 링크 미리보기 */
.link-preview:not(.has-image) {
  padding: 16px;
  background: #f8f9fa;
}
.link-preview.small:not(.has-image) {
  padding: 12px;
}

.link-preview-image {
  background: #f5f5f5; /* 이미지 로딩 전 배경색 */
}

.link-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0; /* flex-shrink 방지 */
}

.link-title {
  font-weight: 600;
  color: #212529;
  font-size: 1rem;
  line-height: 1.3;
  /* 2줄 이상일 경우 말줄임표 */
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp:2;
  -webkit-box-orient: vertical;
}
.link-preview.small .link-title {
  font-size: 0.9rem;
  -webkit-line-clamp: 1;
  line-clamp:1; /* 작은 UI에선 1줄 */
}

.link-description {
  color: #495057;
  font-size: 0.875rem;
  line-height: 1.4;
  /* 3줄 이상일 경우 말줄임표 */
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  line-clamp:3;
  -webkit-box-orient: vertical;
  margin: 0;
}
.link-preview.small .link-description {
  font-size: 0.8rem;
  -webkit-line-clamp: 2;
  line-clamp:2; /* 작은 UI에선 2줄 */
}

.link-url {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #6c757d;
  font-size: 0.85rem;
  margin-top: 8px;
}
.link-icon {
  flex-shrink: 0;
  font-size: 14px;
}
.link-text {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.mention {
  color: #ed664d;
  font-weight: 700;
  cursor: pointer;
  text-decoration: none;
}

.mention:hover {
  font-weight: 700;
  background-color: #fff0ed;
}

/* Swiper Navigation Customization */
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