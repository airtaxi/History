<script setup lang="ts">
import { RouterView } from 'vue-router';
import { useUiStore } from '@/stores/ui';
import PostModal from '@/components/PostModal.vue';
import "./App.css"
const uiStore = useUiStore();
</script>

<template>
  <div class="app-container">
    <main class="main-content">
      <RouterView />
    </main>

    <PostModal v-if="uiStore.isPostEditorOpen" />
    
    <footer class="app-footer">
      <div class="footer-content">
        <div class="footer-links">
          <a href="https://history.cenox.io/terms.html" target="_blank" rel="noopener noreferrer">이용약관</a>
          <span class="divider">|</span>
          <a href="https://history.cenox.io/privacypolicy.html" target="_blank" rel="noopener noreferrer">개인정보처리방침</a>
        </div>
        <p class="copyright">&copy; {{ new Date().getFullYear() }} History. All rights reserved.</p>
      </div>
    </footer>
  </div>
</template>
