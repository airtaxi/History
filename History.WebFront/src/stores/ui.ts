/**
 * @fileoverview UI 상태 관리 스토어 (v2.0.0)
 * * @description
 * History 웹 애플리케이션의 UI 상태를 관리합니다.
 * '공유'와 '리포스트' 개념을 명확히 분리하고, 에디터 상태를 중앙에서 관리합니다.
 * * @version 2.0.0
 * @since 2025-07-03
 */

import { ref } from 'vue'
import { defineStore } from 'pinia'
import type { PostResponseDto } from '@/types'

export const useUiStore = defineStore('ui', () => {
  
  // ==================== 상태 (State) ====================
  
  /**
   * 게시글 작성/공유 에디터의 열림/닫힘 상태
   */
  const isEditorOpen = ref(false)
  
  /**
   * 현재 '공유하기' 모드인지 여부
   */
  const isShareMode = ref(false)
  
  /**
   * 공유할 원본 게시글 데이터
   */
  const shareOriginalPost = ref<PostResponseDto | null>(null)

  // ==================== 액션 (Actions) ====================

  /**
   * 새 게시글 작성을 위해 에디터를 엽니다.
   */
  function openEditorForNewPost() {
    console.log('📝 새 글 작성 에디터 열기');
    isShareMode.value = false;
    shareOriginalPost.value = null;
    isEditorOpen.value = true;
  }

  /**
   * 기존 게시글을 '공유'하기 위해 에디터를 엽니다.
   * @param originalPost - 공유할 원본 게시글 데이터
   */
  function openShareEditor(originalPost: PostResponseDto) {
    if (!originalPost) {
      console.error('❌ 공유할 원본 게시글 데이터가 없습니다.');
      return;
    }
    
    console.log('🔄 공유하기 모드로 에디터 열기', { id: originalPost.id });
    isShareMode.value = true;
    shareOriginalPost.value = originalPost;
    isEditorOpen.value = true;
  }

  /**
   * 에디터를 닫고 모든 관련 상태를 초기화합니다.
   */
  function closeEditor() {
    console.log('❌ 에디터 닫기 및 상태 초기화');
    isEditorOpen.value = false;
    isShareMode.value = false;
    shareOriginalPost.value = null;
  }

  // ==================== 스토어 반환 객체 ====================
  
  return { 
    // State
    isEditorOpen, 
    isShareMode,
    shareOriginalPost,
    
    // Actions
    openEditorForNewPost,
    openShareEditor,
    closeEditor
  }
})