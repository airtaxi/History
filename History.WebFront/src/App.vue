<script setup lang="ts">
import { RouterView, useRoute } from 'vue-router';
import { useUiStore } from '@/stores/ui';
import { computed } from 'vue';
import PostModal from '@/components/PostModal.vue';
import "./App.css" 
import TheHeader from '@/components/layout/TheHeader.vue';


const uiStore = useUiStore();
const route = useRoute();

// footer를 숨길 경로들
const hideFooterRoutes = ['timeline', 'user-profile'];
const shouldShowFooter = computed(() => {
  return !hideFooterRoutes.includes(route.name as string);
});
</script>
<template>
  <TheHeader />
  <div class="app-container">
    <main class="main-content">
      <router-view v-slot="{ Component, route }">
        <keep-alive include="TimelineView">
          <component :is="Component" :key="route.name" />
        </keep-alive>
      </router-view>
    </main>

    <PostModal v-if="uiStore.isEditorOpen" />
  
    <footer v-if="shouldShowFooter" class="app-footer">
      <div class="footer-content">
        <div class="footer-links">
          <a href="/terms.html" target="_blank" class="footer-link">이용약관</a>
          <span class="divider">|</span>
          <a href="/privacypolicy.html" target="_blank" class="footer-link">개인정보처리방침</a>
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

.footer-links .footer-link {
  color: #495057;
  text-decoration: none;
  transition: color 0.2s;
}

.footer-links .footer-link:hover {
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

/* 모바일 반응형 */
@media (max-width: 768px) {
  .app-footer {
    padding: 20px 16px;
    font-size: 0.8rem;
  }
  
  .footer-links .divider {
    margin: 0 8px;
  }
  
  .main-content {
    padding: 0;
  }
}

/* 태블릿 반응형 */
@media (max-width: 1024px) and (min-width: 769px) {
  .app-footer {
    padding: 25px 20px;
  }
}
</style>

