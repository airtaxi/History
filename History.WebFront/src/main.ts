import { createApp } from 'vue';
import App from './App.vue';
import router from './router';
import { createPinia } from 'pinia';
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate';
import { useAuthStore } from '@/stores/auth';
import './assets/fonts.css';

// 개발 환경에서 Friendship API 테스트 유틸리티 로드
if (import.meta.env.DEV) {
  import('@/utils/friendship-api-test');
} 

const app = createApp(App);

const pinia = createPinia();
pinia.use(piniaPluginPersistedstate);

app.use(pinia);  
app.use(router);
app.mount('#app');

const authStore = useAuthStore();
if (authStore.isAuthenticated) {

  authStore.fetchMe?.(); 
}
