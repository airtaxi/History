<template>
  <div class="post-header">
    <!-- Author info (avatar, nickname) -->
    <div class="author-info">
      <img :src="user.avatar" alt="User Avatar" class="avatar" />
      <span class="nickname">{{ user.nickname }}</span>
    </div>
    <!-- Created time -->
    <span class="created-at">{{ formatRelativeTime(createdAt) }}</span>
    <!-- More options menu -->
    <div class="more-options">
      <button @click="toggleDropdown">...</button>
      <div v-if="showDropdown" class="dropdown-menu">
        <button v-if="canEdit" @click="$emit('edit')">Edit</button>
        <button v-if="canEdit" @click="$emit('delete')">Delete</button>
        <button @click="$emit('report')">Report</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

interface User {
  avatar: string;
  nickname: string;
}

const props = defineProps({
  user: {
    type: Object as () => User,
    required: true,
  },
  createdAt: {
    type: String,
    required: true,
  },
  canEdit: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(['edit', 'delete', 'report']);

const showDropdown = ref(false);

const toggleDropdown = () => {
  showDropdown.value = !showDropdown.value;
};

// Dummy function for formatRelativeTime - replace with actual utility
const formatRelativeTime = (dateString: string) => {
  const date = new Date(dateString);
  const now = new Date();
  const diff = now.getTime() - date.getTime();
  const seconds = Math.floor(diff / 1000);
  const minutes = Math.floor(seconds / 60);
  const hours = Math.floor(minutes / 60);
  const days = Math.floor(hours / 24);

  if (days > 0) {
    return `${days} days ago`;
  } else if (hours > 0) {
    return `${hours} hours ago`;
  } else if (minutes > 0) {
    return `${minutes} minutes ago`;
  } else {
    return `${seconds} seconds ago`;
  }
};
</script>

<style scoped>
.post-header {
  display: flex;
  align-items: center;
  padding: 10px;
  border-bottom: 1px solid #eee;
}

.author-info {
  display: flex;
  align-items: center;
}

.avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  margin-right: 10px;
}

.nickname {
  font-weight: bold;
}

.created-at {
  margin-left: auto;
  color: #888;
  font-size: 0.9em;
}

.more-options {
  position: relative;
  margin-left: 10px;
}

.dropdown-menu {
  position: absolute;
  top: 100%;
  right: 0;
  background-color: white;
  border: 1px solid #ccc;
  border-radius: 5px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  z-index: 10;
}

.dropdown-menu button {
  display: block;
  width: 100%;
  padding: 8px 12px;
  border: none;
  background: none;
  text-align: left;
  cursor: pointer;
}

.dropdown-menu button:hover {
  background-color: #f0f0f0;
}
</style>
