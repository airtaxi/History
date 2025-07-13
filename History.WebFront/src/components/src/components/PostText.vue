<template>
  <div class="post-text-container">
    <p ref="textElement" :class="{ collapsed: isCollapsed }" class="post-text">
      <template v-for="(chunk, i) in splitTextWithLinksAndMentions(text)" :key="i">
        <a
          v-if="chunk.type === 'link'"
          :href="chunk.text.startsWith('www.') ? 'https://' + chunk.text : chunk.text"
          target="_blank"
          rel="noopener noreferrer"
          class="text-link"
          @click.stop
        >{{ chunk.text }}</a>
        <span v-else-if="chunk.type === 'mention'" class="mention" @click.stop="$emit('navigate-to-profile', chunk.text)">{{ chunk.text }}</span>
        <span v-else>{{ chunk.text }}</span>
      </template>
    </p>
    <button v-if="showReadMore" @click="toggleCollapse" class="read-more-btn">
      {{ isCollapsed ? '더보기' : '접기' }}
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, nextTick, defineProps, defineEmits } from 'vue';
import { splitTextWithLinksAndMentions } from '@/components/src/utils/textUtils';

const props = defineProps<{
  text: string;
}>();

defineEmits(['navigate-to-profile']);

const textElement = ref<HTMLElement | null>(null);
const isCollapsed = ref(true);
const showReadMore = ref(false);

const checkTextOverflow = async () => {
  await nextTick();
  if (textElement.value) {
    const p = textElement.value;
    const style = window.getComputedStyle(p);
    let lineHeight = parseFloat(style.lineHeight);
    if (isNaN(lineHeight) || style.lineHeight === 'normal') {
      lineHeight = parseFloat(style.fontSize) * 1.2;
    }
    const fiveLinesHeight = lineHeight * 5;

    if (p.scrollHeight > fiveLinesHeight) {
      showReadMore.value = true;
    } else {
      showReadMore.value = false;
      isCollapsed.value = false; // 컨텐츠가 5줄 미만이면 항상 펼쳐진 상태
    }
  }
};

const toggleCollapse = () => {
  isCollapsed.value = !isCollapsed.value;
};

onMounted(() => {
  checkTextOverflow();
});

</script>

<style scoped>
.post-text-container {
  position: relative;
}

.post-text {
  white-space: pre-wrap;
  word-break: break-word;
  margin: 0;
  transition: max-height 0.3s ease;
}

.post-text.collapsed {
  max-height: 105px; /* 5 lines (21px * 5) */
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 5;
  -webkit-box-orient: vertical;
}

.read-more-btn {
  background: none;
  border: none;
  color: #8c8c8c;
  cursor: pointer;
  font-weight: bold;
  padding: 4px 0;
  margin-top: 4px;
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

.text-link {
  color: #0066cc;
  word-break: break-all;
  text-decoration: none;
}

.text-link:hover {
  text-decoration: underline;
}
</style>
