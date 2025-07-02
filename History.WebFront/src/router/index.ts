import { createRouter, createWebHistory } from 'vue-router'
import TimelineView from '@/views/TimelineView.vue'
import { useAuthStore } from '@/stores/auth' // Pinia store

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'timeline',
      component: TimelineView,
      meta: { requiresAuth: true },
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/accounts/LoginView.vue')
    },
    {
      path: '/profile-setup',
      name: 'profile-setup',
      component: () => import('@/views/accounts/ProfilemakeView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/user/:userId',
      name: 'user-profile',
      component: () => import('@/views/UserProfileView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/post/:postId',
      name: 'post-detail',
      component: () => import('@/views/PostDetailView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/post/edit/:postId',
      name: 'EditPost',
      component: () => import('@/views/EditPostView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/me',
      name: 'my-page',
      component: () => import('@/views/MyPageView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/notifications',
      name: 'NotificationsView',
      component: () => import('@/views/NotificationsView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/user-settings',
      name: 'UserSettingsView',
      component: () => import('@/views/SettingsView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/terms',
      name: 'TermsView',
      component: () => import('@/views/TermsView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/privacy',
      name: 'PrivacyView',
      component: () => import('@/views/PrivacyView.vue'),
      meta: { requiresAuth: true },
    },
  ]
})

router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next('/login')
  } else {
    next()
  }
})

export default router
