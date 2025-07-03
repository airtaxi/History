<template>
    <div class="detail-item">
      <!-- 게시글 본문 -->
      <PostCard :post="post" :show-actions="true" :profile-image-map="profileImageMap" />
  
      <!-- 댓글 리스트 -->
      <div class="comments-section">
        <div class="comments-header">댓글 {{ comments.length }}</div>
  
        <CommentItem 
          v-for="comment in comments" 
          :key="comment.id"
          :comment="comment"
          :profile-image-url="profileImageMap[comment.user.userId] || '/src/assets/images/default_profile_image.jpg'"
          @mention-user="handleMentionUser"
          @delete-comment="deleteMyComment"
          @like-comment="handleLikeComment"
          @update-comment="handleUpdateComment"
        />
  
        <!-- 댓글 작성창 -->
        <CreateComment 
          :post-id="post.id" 
          @comment-created="refreshComments"
          ref="createCommentRef" 
        />
      </div>
    </div>
  </template>
  
  <script setup lang="ts">
  import { ref, onMounted } from 'vue';
  import type { PostResponseDto, CommentResponseDto, UserDto } from '@/types';
  import apiClient from '@/api';
  
  import PostCard from './PostCard.vue';
  import CommentItem from './CommentItem.vue';
  import CreateComment from './CreateComment.vue';
  
  const props = defineProps<{
    post: PostResponseDto;
  }>();
  
  const comments = ref<CommentResponseDto[]>([]);
  const profileImageMap = ref<Record<string, string>>({});
  const createCommentRef = ref<InstanceType<typeof CreateComment> | null>(null);
  
  // --- 프로필 이미지 blob URL 생성
  const getMediaBlobUrl = async (mediaId: string) => {
    try {
      const response = await apiClient.get(`/api/Media/${mediaId}`, {
        responseType: 'blob',
      });
      const contentType = response.headers['content-type'];
      if (!contentType.startsWith('image')) return '';
      return URL.createObjectURL(response.data);
    } catch {
      return '';
    }
  };
  
  // --- 댓글 작성자들의 프로필 이미지 준비
  const prepareProfileImageMapForUsers = async (users: (UserDto | undefined)[]) => {
    const userIds = new Set<string>();
    users.forEach((user) => {
      if (user?.profileThumbnailMediaId) {
        userIds.add(user.userId);
      }
    });
  
    for (const userId of userIds) {
      if (profileImageMap.value[userId]) continue;
  
      const user = users.find((u) => u?.userId === userId)!;
      const blobUrl = await getMediaBlobUrl(user.profileThumbnailMediaId!);
      profileImageMap.value[userId] = blobUrl || '/src/assets/images/default_profile_image.jpg';
    }
  };
  
  // --- 댓글 불러오기 (최대 5개)
  const fetchComments = async () => {
    try {
      const res = await apiClient.get<CommentResponseDto[]>(
        `/api/Comment/${props.post.id}?limit=5`
      );
      comments.value = res.data;
      await prepareProfileImageMapForUsers(res.data.map(c => c.user));
    } catch (err) {
      console.error('댓글 불러오기 실패:', err);
    }
  };
  
  // --- 댓글 새로고침
  const refreshComments = async () => {
    await fetchComments();
  };
  
  // --- 댓글 좋아요
  const handleLikeComment = async (commentId: string) => {
    try {
      const res = await apiClient.post(`/api/Comment/${commentId}/like`);
      const updated = res.data;
      const index = comments.value.findIndex(c => c.id === commentId);
      if (index !== -1) {
        comments.value[index] = updated;
      }
    } catch {
      alert('댓글 좋아요 실패');
    }
  };
  
  // --- 댓글 수정
  const handleUpdateComment = async ({ commentId, newText }: { commentId: string, newText: string }) => {
    try {
      const formData = new FormData();
      const jsonData = JSON.stringify([{ $type: 'text', Text: newText }]);
      formData.append('JsonData', jsonData);
      const res = await apiClient.put(`/api/Comment/${commentId}`, formData);
      const updated = res.data;
      const index = comments.value.findIndex(c => c.id === commentId);
      if (index !== -1) comments.value[index] = updated;
    } catch {
      alert('댓글 수정 실패');
    }
  };
  
  // --- 댓글 삭제
  const deleteMyComment = async (commentId: string) => {
    try {
      await apiClient.delete(`/api/Comment/${commentId}`);
      comments.value = comments.value.filter(c => c.id !== commentId);
    } catch {
      alert('댓글 삭제 실패');
    }
  };
  
  // --- 멘션 처리
  const handleMentionUser = (nickname: string) => {
    createCommentRef.value?.addMention(nickname);
  };
  
  // 최초 댓글 로딩
  onMounted(() => {
    fetchComments();
  });
  </script>
  
  <style scoped>
  .detail-item {
    margin-bottom: 24px;
    background-color: white;
    border: 1px solid #ddd;
    border-radius: 8px;
    overflow: hidden;
  }
  
  .comments-section {
    border-top: 1px solid #eee;
    padding: 12px 16px;
  }
  
  .comments-header {
    font-weight: bold;
    font-size: 1rem;
    margin-bottom: 8px;
  }
  </style>
  