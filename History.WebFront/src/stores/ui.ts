/**
 * @fileoverview UI 상태 관리 스토어
 * 
 * @description
 * History 웹 애플리케이션의 사용자 인터페이스 상태를 관리하는 Pinia 스토어입니다.
 * 게시글 작성 에디터와 리포스트 기능의 상태를 중앙에서 관리합니다.
 * 
 * @features
 * - 게시글 작성 에디터 열기/닫힘 상태 관리
 * - 리포스트 모드 상태 관리
 * - 리포스트할 원본 게시글 정보 저장
 * - 전역 UI 상태 동기화
 * 
 * @version 1.2.0
 * @since 2025-01-27
 */

// src/stores/ui.ts
import { ref } from 'vue'
import { defineStore } from 'pinia'
import type { PostResponseDto } from '@/types'

/**
 * UI 상태 관리 스토어 정의
 * 
 * @store useUiStore
 * @description
 * 애플리케이션의 UI 상태를 중앙에서 관리하는 Pinia 스토어입니다.
 * 게시글 작성 및 리포스트 관련 상태를 포함합니다.
 * 
 * @example
 * // 컴포넌트에서 스토어 사용
 * import { useUiStore } from '@/stores/ui'
 * 
 * const uiStore = useUiStore()
 * uiStore.openPostEditor()
 * 
 * @returns {Object} 스토어 객체 (상태값들과 액션 함수들)
 */
export const useUiStore = defineStore('ui', () => {
  
  // ==================== 반응형 상태 변수들 ====================
  
  /**
   * 게시글 작성 에디터의 열림/닫힘 상태
   * @type {import('vue').Ref<boolean>}
   * @description 
   * - true: 게시글 작성 에디터가 확장된 상태 (편집 모드)
   * - false: 게시글 작성 에디터가 축소된 상태 (컴팩트 모드)
   * 
   * @default false
   */
  const isPostEditorOpen = ref(false)
  
  /**
   * 리포스트할 원본 게시글 데이터
   * @type {import('vue').Ref<PostResponseDto | null>}
   * @description
   * 리포스트 모드에서 참조할 원본 게시글의 전체 정보를 저장합니다.
   * 리포스트 모드가 아닐 때는 null입니다.
   * 
   * @default null
   */
  const repostOriginalPost = ref<PostResponseDto | null>(null)
  
  /**
   * 현재 리포스트 모드 여부
   * @type {import('vue').Ref<boolean>}
   * @description
   * - true: 리포스트 모드 (원본 게시글을 리포스트하는 상태)
   * - false: 일반 게시글 작성 모드
   * 
   * @default false
   */
  const isRepostMode = ref(false)

  // ==================== 액션 함수들 ====================

  /**
   * 일반 게시글 작성 에디터를 여는 함수
   * 
   * @function openPostEditor
   * @returns {void}
   * 
   * @description
   * 게시글 작성 에디터를 확장된 상태로 변경합니다.
   * 일반 게시글 작성 모드로 설정됩니다. (리포스트 모드 아님)
   * 
   * @example
   * // 게시글 작성 버튼 클릭 시
   * const uiStore = useUiStore()
   * uiStore.openPostEditor()
   * 
   * @since 1.0.0
   */
  function openPostEditor() {
    console.log('📝 일반 게시글 작성 에디터 열기');
    isPostEditorOpen.value = true
  }

  /**
   * 게시글 작성 에디터를 닫고 모든 관련 상태를 초기화하는 함수
   * 
   * @function closePostEditor
   * @returns {void}
   * 
   * @description
   * 게시글 작성 에디터를 축소된 상태로 변경하고,
   * 리포스트 관련 모든 상태를 초기화합니다.
   * 
   * **초기화되는 상태:**
   * - 에디터 열림 상태 → false
   * - 리포스트 모드 → false  
   * - 원본 게시글 정보 → null
   * 
   * @example
   * // 게시글 작성 완료 또는 취소 시
   * const uiStore = useUiStore()
   * uiStore.closePostEditor()
   * 
   * @since 1.0.0
   */
  function closePostEditor() {
    console.log('❌ 게시글 작성 에디터 닫기 및 상태 초기화');
    isPostEditorOpen.value = false
    repostOriginalPost.value = null
    isRepostMode.value = false
  }

  /**
   * 리포스트 모드로 게시글 작성 에디터를 여는 함수
   * 
   * @function openRepostEditor
   * @param {PostResponseDto} originalPost - 리포스트할 원본 게시글 데이터
   * @returns {void}
   * 
   * @description
   * 특정 게시글을 리포스트하기 위한 에디터를 엽니다.
   * 원본 게시글 정보를 저장하고 리포스트 모드로 설정합니다.
   * 
   * **설정되는 상태:**
   * - 에디터 열림 상태 → true
   * - 리포스트 모드 → true
   * - 원본 게시글 정보 → 전달받은 게시글 데이터
   * 
   * @param {PostResponseDto} originalPost - 리포스트할 원본 게시글 객체
   * @param {string} originalPost.id - 원본 게시글 ID
   * @param {UserResponseDto} originalPost.user - 원본 게시글 작성자 정보
   * @param {Array} originalPost.contents - 원본 게시글 콘텐츠 배열
   * @param {string} originalPost.createdAt - 원본 게시글 작성 시간
   * 
   * @throws {Error} originalPost가 null/undefined인 경우 에러 발생
   * 
   * @example
   * // PostCard 컴포넌트에서 공유 버튼 클릭 시
   * const uiStore = useUiStore()
   * const originalPost = { id: '123', user: {...}, contents: [...] }
   * uiStore.openRepostEditor(originalPost)
   * 
   * @since 1.2.0
   */
  function openRepostEditor(originalPost: PostResponseDto) {
    if (!originalPost) {
      console.error('❌ 원본 게시글 데이터가 없습니다.');
      throw new Error('리포스트할 원본 게시글 데이터가 필요합니다.');
    }
    
    console.log('🔄 리포스트 모드로 에디터 열기');
    console.log('📋 원본 게시글 정보:', {
      id: originalPost.id,
      author: originalPost.user?.nickname,
      contentCount: originalPost.contents?.length || 0
    });
    
    isPostEditorOpen.value = true
    isRepostMode.value = true
    repostOriginalPost.value = originalPost
  }

  // ==================== 스토어 반환 객체 ====================

    /**
   * 스토어에서 외부로 노출할 상태와 액션들
   * 
   * @returns {Object} 스토어 인터페이스 객체
   * @property {Ref<boolean>} isPostEditorOpen - 에디터 열림 상태
   * @property {Ref<boolean>} isRepostMode - 리포스트 모드 상태  
   * @property {Ref<PostResponseDto | null>} repostOriginalPost - 원본 게시글 데이터
   * @property {Function} openPostEditor - 일반 에디터 열기 함수
   * @property {Function} closePostEditor - 에디터 닫기 함수
   * @property {Function} openRepostEditor - 리포스트 에디터 열기 함수
   */
  return { 
    // 상태 변수들
    isPostEditorOpen, 
    isRepostMode,
    repostOriginalPost,
    
    // 액션 함수들
    openPostEditor, 
    closePostEditor,
    openRepostEditor
  }
})