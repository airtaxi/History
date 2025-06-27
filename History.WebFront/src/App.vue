<script setup lang="ts">
import { RouterView } from 'vue-router';
import { useUiStore } from '@/stores/ui';
import PostModal from '@/components/PostModal.vue';

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

<style scoped>

.app-container {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background-color: #f8f9fa;
}

.main-content {
  flex-grow: 1;
}

.app-footer {
  background-color: #ffffff;
  color: #868e96;
  padding: 30px 24px;
  border-top: 1px solid #e9ecef;
  flex-shrink: 0;
  font-size: 0.85rem;
  text-align: center;
}

.footer-content {
  max-width: 1200px;
  margin: 0 auto;
}

.footer-links {
  margin-bottom: 8px;
}

.footer-links a {
  color: #495057;
  text-decoration: none;
  transition: color 0.2s;
}

.footer-links a:hover {
  color: #000;
  text-decoration: underline;
}

.footer-links .divider {
  margin: 0 12px;
  color: #dee2e6;
}

.copyright {
  margin: 0;
  color: #adb5bd;
}
</style>