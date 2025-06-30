<script setup lang="ts">
import { RouterView } from 'vue-router';
import { useUiStore } from '@/stores/ui';
import PostModal from '@/components/PostModal.vue';
import "./App.css" // 전체 프레임에 대한 전역 css는 App.css에서 모두 관리합니다.
import TheHeader from '@/components/layout/TheHeader.vue';


const uiStore = useUiStore();
</script>
<template>
  <TheHeader />
  <div class="app-container">
    <main class="main-content">
      <RouterView />
    </main>

    <PostModal v-if="uiStore.isPostEditorOpen" />

  </div>
</template>

<style scoped>

</style>
