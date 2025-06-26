import { createRouter, createWebHistory } from 'vue-router'
// 모든 import 경로를 '@'로 시작하도록 수정합니다.
import TimelineView from '@/views/TimelineView.vue' 

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'timeline',
      component: TimelineView
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/accounts/LoginView.vue') // 수정
    },
    {
      path: '/profile-setup', 
      name: 'profile-setup',
      component: () => import('@/views/accounts/ProfilemakeView.vue')
    },
    {
      path: '/user/:userId',
      name: 'user-profile',
      component: () => import('@/views/UserProfileView.vue') // 수정
    },
    {
      path: '/post/:postId',
      name: 'post-detail',
      component: () => import('@/views/PostDetailView.vue') // 수정
    },
    {
      path: '/me',
      name: 'my-page',
      component: () => import('@/views/MyPageView.vue')
    },
    {
      path: '/post/:postId',
      name: 'post-detail',
      component: () => import('@/views/PostDetailView.vue')
    },
    {
      path: '/post/edit/:postId',
      name: 'EditPost',
      component: () => import('@/views/EditPostView.vue'),
    },
    {
      path: '/notifications',
      name: 'NotificationsView',
      component: () => import('@/views/NotificationsView.vue')
    },
    {
      path: '/user-settings',
      name: 'UserSettingsView',
      component: () => import('@/views/SettingsView.vue')
    },
  ]
})

export default router