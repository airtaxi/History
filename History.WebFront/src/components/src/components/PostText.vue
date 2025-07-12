<template>
  <p style="white-space: pre-wrap; word-break: break-word;">
    <template v-for="(chunk, i) in splitTextWithLinksAndMentions(text)" :key="i">
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
</template>

<script setup lang="ts">
import { defineProps, defineEmits } from 'vue';

defineProps<{
  text: string;
}>();

defineEmits(['navigate-to-profile']);

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
</script>

<style scoped>
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
</style>
