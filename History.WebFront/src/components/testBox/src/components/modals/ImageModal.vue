<!--
 * ImageModal.vue
 *
 * 이 컴포넌트는 게시물 내 이미지를 전체 화면으로 확대하여 보여주는 모달입니다.
 * 여러 이미지를 슬라이드하여 볼 수 있도록 Swiper 라이브러리를 통합합니다.
 *
 * @props {
 *   show: boolean - 모달의 표시 여부.
 *   mediaSource: Array<{ src: string; type: string; mimeType?: string }> - 표시할 미디어 소스 배열.
 *   initialSlideIndex: number - 모달이 열릴 때 처음에 보여줄 슬라이드의 인덱스.
 * }
 * @emits {
 *   close: 모달을 닫을 때 발생.
 * }
-->
<template>
  <Teleport to="body">
    <div v-if="show" class="image-modal" @click="$emit('close')">
      <button @click.stop="$emit('close')" class="modal-close-button">×</button>
      <div class="modal-swiper-container" @click.stop>
        <Swiper
          v-if="mediaSource.length > 0"
          :initialSlide="initialSlideIndex"
          :navigation="true"
          :pagination="{ type: 'fraction' }"
          :loop="mediaSource.length > 1"
          :modules="modules"
          class="modal-swiper"
        >
          <SwiperSlide v-for="media in mediaSource" :key="media.src">
            <video
              v-if="media.type === 'video'"
              controls
              class="modal-image"
              :key="'video-' + media.src"
            >
              <source :src="media.src" :type="media.mimeType" />
              브라우저가 video 태그를 지원하지 않습니다.
            </video>

            <img
              v-else
              :src="media.src"
              alt="확대 이미지"
              class="modal-image"
              :key="'image-' + media.src"
            />
          </SwiperSlide>
        </Swiper>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { defineProps, defineEmits } from 'vue';
import { Swiper, SwiperSlide } from 'swiper/vue';
import { Navigation, Pagination } from 'swiper/modules';

const props = defineProps<{
  show: boolean;
  mediaSource: Array<{ src: string; type: string; mimeType?: string }>;
  initialSlideIndex: number;
}>();

const emit = defineEmits(['close']);

const modules = [Navigation, Pagination];
</script>

<style scoped>
.image-modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  cursor: zoom-out;
  flex-direction: column;
}

.modal-swiper-container {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.modal-swiper {
  width: 90%;
  height: 90%;
}

.modal-image {
  width: 100%;
  height: 100%;
  object-fit: contain;
  border-radius: 0;
  box-shadow: none;
}

.modal-close-button {
  position: absolute;
  top: 20px;
  right: 20px;
  background: rgba(0,0,0,0.5);
  color: white;
  border: none;
  border-radius: 50%;
  width: 40px;
  height: 40px;
  font-size: 24px;
  line-height: 40px;
  text-align: center;
  cursor: pointer;
  z-index: 10001; /* Swiper 위에 오도록 */
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
  background-color: #ed664d ;
}
</style>
