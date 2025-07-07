<template>
  <Teleport to="body">
    <div v-if="show" class="reaction-popup" :style="popupStyle" @click.self="$emit('close')">
      <div class="reaction-options">
        <span
          v-for="reaction in reactions"
          :key="reaction.type"
          class="reaction-item"
          @click="$emit('select-reaction', reaction.type)"
        >
          {{ reaction.emoji }}
        </span>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps({
  show: {
    type: Boolean,
    required: true,
  },
  x: {
    type: Number,
    default: 0,
  },
  y: {
    type: Number,
    default: 0,
  },
});

const emit = defineEmits(['close', 'select-reaction']);

const reactions = [
  { type: 'like', emoji: '👍' },
  { type: 'love', emoji: '❤️' },
  { type: 'haha', emoji: '😂' },
  { type: 'wow', emoji: '😮' },
  { type: 'sad', emoji: '😢' },
  { type: 'angry', emoji: '😡' },
];

const popupStyle = computed(() => ({
  position: 'absolute',
  left: `${props.x}px`,
  top: `${props.y}px`,
  transform: 'translate(-50%, -100%)', // Adjust to position above the trigger
}));
</script>

<style scoped>
.reaction-popup {
  z-index: 1001;
}

.reaction-options {
  background: white;
  border-radius: 50px;
  padding: 5px 10px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.2);
  display: flex;
  gap: 5px;
}

.reaction-item {
  font-size: 1.5em;
  cursor: pointer;
  padding: 5px;
  transition: transform 0.1s ease-in-out;
}

.reaction-item:hover {
  transform: scale(1.2);
}
</style>
