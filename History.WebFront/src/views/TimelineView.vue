<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import { defineAsyncComponent } from 'vue'
import apiClient from '@/api'
import type { PostResponseDto } from '@/types'

// 컴포넌트 import (항상 최상단!)
const PostCard = defineAsyncComponent(() => import('@/components/PostCard.vue'))
import RightSidebar from '@/components/layout/RightSidebar.vue'
import CreatePost from '@/components/CreatePost.vue'

// --- 상태 관리 ---
const posts = ref<PostResponseDto[]>([])
const isLoading = ref(true)
const isLoadingMore = ref(false)
const noMorePosts = ref(false)
const loadMoreSentinel = ref<HTMLElement | null>(null)
const profileImageMap = ref<Record<string, string>>({})
const isDetailModalOpen = ref(false)
const selectedPostId = ref('')

const openModalWithPost = (postId: string) => {
  selectedPostId.value = postId
  isDetailModalOpen.value = true
}

// 스크롤 위로 올리는 버튼
const showScrollTopBtn = ref(false)
const handleScroll = () => {
  showScrollTopBtn.value = window.scrollY > 200
}
const scrollToTop = () => {
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

let observer: IntersectionObserver

onMounted(() => {
  fetchTimeline()

  nextTick(() => {
    observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting) loadMorePosts()
      },
      { rootMargin: '200px' }
    )
    if (loadMoreSentinel.value) observer.observe(loadMoreSentinel.value)
  })

  window.addEventListener('scroll', handleScroll)
})

onUnmounted(() => {
  observer?.disconnect()
  window.removeEventListener('scroll', handleScroll)
})

const getMediaBlobUrl = async (mediaId: string) => {
  try {
    const response = await apiClient.get(`/api/Media/${mediaId}`, { responseType: 'blob' })
    const contentType = response.headers['content-type']
    if (!contentType.startsWith('image')) return ''
    return URL.createObjectURL(response.data)
  } catch {
    return ''
  }
}

const prepareProfileImageMap = async (postList: PostResponseDto[]) => {
  const map: Record<string, string> = {}
  const userIds = new Set<string>()

  postList.forEach((p) => {
    userIds.add(p.user.userId)
    if ((p as any).isRepost && (p as any).parentPost?.user) {
      userIds.add((p as any).parentPost.user.userId)
    }
  })

  for (const uid of userIds) {
    if (profileImageMap.value[uid]) continue

    let user = postList.find((p) => p.user.userId === uid)?.user
    if (!user) {
      for (const post of postList) {
        if ((post as any).isRepost && (post as any).parentPost?.user?.userId === uid) {
          user = (post as any).parentPost.user
          break
        }
      }
    }

    if (user?.profileThumbnailMediaId) {
      const blobUrl = await getMediaBlobUrl(user.profileThumbnailMediaId)
      map[uid] = blobUrl || '/src/assets/images/default_profile_image.jpg'
    }
  }

  profileImageMap.value = { ...profileImageMap.value, ...map }
}

const fetchTimeline = async () => {
  try {
    isLoading.value = true
    posts.value = []
    noMorePosts.value = false

    let hasMore = true
    let fromId = null

    while (hasMore && posts.value.length < 10) {
      const { data }: { data: PostResponseDto[] } = await apiClient.get('/api/Post/timeline', {
        params: fromId ? { from: fromId } : {},
      })

      const pagePosts: PostResponseDto[] = data.filter(
        (post: PostResponseDto) => !post.isRepost || (post.isRepost && post.parentPost !== null)
      )

      if (data.length === 0) {
        hasMore = false
        break
      }

      if (pagePosts.length > 0) {
        posts.value.push(...pagePosts)
        await prepareProfileImageMap(pagePosts)
      }

      fromId = data[data.length - 1]?.id
    }

    if (posts.value.length > 10) {
      posts.value = posts.value.slice(0, 10)
    }
  } catch (error) {
    console.error('타임라인 로딩 실패:', error)
  } finally {
    isLoading.value = false
  }
}

const loadMorePosts = async () => {
  if (isLoadingMore.value || noMorePosts.value) return

  const lastPost = posts.value[posts.value.length - 1]
  if (!lastPost) return

  try {
    isLoadingMore.value = true
    let fromId = lastPost.id
    let addedCount = 0
    const targetAddCount = 5

    while (addedCount < targetAddCount && !noMorePosts.value) {
      const response = await apiClient.get<PostResponseDto[]>('/api/Post/timeline', {
        params: { from: fromId },
      })

      if (response.data.length === 0) {
        noMorePosts.value = true
        break
      }

      const newPosts: PostResponseDto[] = response.data.filter(
        (post: PostResponseDto) => !post.isRepost || (post.isRepost && post.parentPost !== null)
      )

      if (newPosts.length > 0) {
        posts.value.push(...newPosts)
        await prepareProfileImageMap(newPosts)
        addedCount += newPosts.length
      }

      fromId = response.data[response.data.length - 1].id

      if (response.data.length > 0 && newPosts.length === 0) {
        continue
      }
    }
  } catch (error) {
    console.error('추가 타임라인 로딩 실패:', error)
  } finally {
    isLoadingMore.value = false
  }
}

const handlePostCreated = async () => {
  try {
    const response: { data: PostResponseDto[] } = await apiClient.get<PostResponseDto[]>('/api/Post/timeline')
    const newPosts: PostResponseDto[] = response.data.filter(
      (post: PostResponseDto) => !post.isRepost || (post.isRepost && post.parentPost !== null)
    )

    for (let i = newPosts.length - 1; i >= 0; i--) {
      const post = newPosts[i]
      const isDuplicate = posts.value.some((p) => p.id === post.id)
      if (!isDuplicate) {
        posts.value.unshift(post)
      }
    }
  } catch (error) {
    console.error('글 작성 후 타임라인 갱신 실패:', error)
  }
}
</script>

<template>
  <div class="timeline-layout">
    <main class="main-content">
      <div class="feed-column">
        <CreatePost @post-created="handlePostCreated" />

        <div v-if="isLoading" class="loading-indicator">
          <div class="spinner"></div>
        </div>

        <div v-else class="post-list">
          <PostCard
            v-for="post in posts"
            :key="post.id"
            :post="post"
            :profile-image-map="profileImageMap"
            @open-detail="openModalWithPost"
          />
        </div>

        <div ref="loadMoreSentinel" class="sentinel"></div>

        <div v-if="isLoadingMore" class="loading-indicator">
          <div class="spinner"></div>
        </div>

        <div v-if="noMorePosts && !isLoading" class="end-of-feed">모든 글을 불러왔습니다.</div>
      </div>
      <RightSidebar />
    </main>

    <button v-if="showScrollTopBtn" class="scroll-top-button" @click="scrollToTop">⬆ 맨 위로</button>
  </div>
</template>

<style scoped>
/* 기존 스타일 그대로 */
.timeline-layout {
  background-color: #f8f9fa;
  min-height: 100vh;
}
.main-content {
  display: flex;
  justify-content: center;
  gap: 24px;
  width: 100%;
  max-width: 1024px;
  margin: 24px auto;
  padding: 0 24px;
}
.feed-column {
  flex: 1;
  max-width: 620px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.post-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.loading-indicator {
  text-align: center;
  padding: 40px;
}
.spinner {
  border: 4px solid #f3f3f3;
  border-top: 4px solid #ed664d;
  border-radius: 50%;
  width: 40px;
  height: 40px;
  animation: spin 1s linear infinite;
  margin: 0 auto;
}
@keyframes spin {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
}
.end-of-feed {
  text-align: center;
  color: #888;
  padding: 20px;
}
.sentinel {
  height: 1px;
}
.scroll-top-button {
  position: fixed;
  bottom: 24px;
  left: 24px;
  background-color: #ed664d;
  color: white;
  padding: 10px 16px;
  border: none;
  border-radius: 24px;
  font-weight: bold;
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
  transition: background-color 0.3s ease;
  z-index: 1000;
}
.scroll-top-button:hover {
  background-color: #e85b3e;
}
</style>
