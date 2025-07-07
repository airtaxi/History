<template>
  <div class="post-content">
    <p class="text-content">{{ contents.text }}</p>
    <div v-if="contents.media && contents.media.length > 0" class="media-slider">
      <!-- Swiper implementation would go here -->
      <div v-for="mediaItem in contents.media" :key="mediaItem.id" class="media-item">
        <img
          v-if="mediaItem.type === 'image'"
          :src="mediaUrlMap[mediaItem.id]"
          @click="$emit('open-media-modal', mediaItem.id)"
          alt="Post Image"
        />
        <video
          v-else-if="mediaItem.type === 'video'"
          :src="mediaUrlMap[mediaItem.id]"
          controls
        ></video>
      </div>
    </div>
    <div v-if="contents.externalUrl" class="external-link-preview">
      <!-- External link preview UI -->
      <a :href="contents.externalUrl" target="_blank">{{ contents.externalUrl }}</a>
    </div>
  </div>
</template>

<script setup lang="ts">
interface Content {
  text: string;
  media?: Array<{ id: string; type: string }>;
  externalUrl?: string;
}

interface MediaUrlMap {
  [key: string]: string;
}

const props = defineProps({
  contents: {
    type: Object as () => Content,
    required: true,
  },
  mediaUrlMap: {
    type: Object as () => MediaUrlMap,
    required: true,
  },
});

const emit = defineEmits(['open-media-modal', 'navigate-to-profile']);

// Logic for parsing links and @mentions in text content would go here
// For now, it's a simple text display
</script>

<style scoped>
.post-content {
  padding: 10px;
}

.media-slider {
  margin-top: 10px;
  /* Basic styling, Swiper would handle actual slider behavior */
  display: flex;
  overflow-x: auto;
}

.media-item {
  flex: 0 0 auto;
  margin-right: 10px;
}

.media-item img,
.media-item video {
  max-width: 100%;
  height: auto;
  display: block;
}

.external-link-preview {
  margin-top: 10px;
  border: 1px solid #eee;
  padding: 10px;
  border-radius: 5px;
}
</style>
