<script setup lang="ts">
import { RouterView } from 'vue-router';
import { useUiStore } from '@/stores/ui';
import PostModal from '@/components/PostModal.vue';
import "./App.css"
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
