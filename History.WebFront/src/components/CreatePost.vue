<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useUiStore } from '@/stores/ui';
import apiClient from '@/api';
import { defineEmits } from 'vue';
import "./CreatePost.css"

/**
 * CreatePost 컴포넌트의 props와 emits 정의
 */
const uiStore = useUiStore();
const emit = defineEmits(['post-created']);

// ==================== 반응형 상태 변수들 ====================

/**
 * 게시글 텍스트 내용
 * @type {import('vue').Ref<string>}
 */
const newPostText = ref('');

/**
 * 게시글 공개 설정 옵션
 * - OnlyMe: 나만 보기
 * - SelectedUsers: 특정 친구 공개
 * - UnselectedUsers: 특정 친구 비공개
 * - Friends: 친구 공개
 * - FriendsOfFriends: 친구의 친구 공개
 * - Everyone: 전체 공개
 * @type {import('vue').Ref<string>}
 */
const discoveryOption = ref('Friends');

/**
 * 업로드할 파일들의 배열
 * @type {import('vue').Ref<File[]>}
 */
const attachedFiles = ref<File[]>([]);

/**
 * 파일 미리보기를 위한 URL과 비디오 여부 정보
 * @type {import('vue').Ref<Array<{url: string, isVideo: boolean}>>}
 */
const previewItems = ref<{ url: string, isVideo: boolean }[]>([]);

/**
 * 첨부할 링크 URL
 * @type {import('vue').Ref<string>}
 */
const attachedLink = ref('');

/**
 * 특정 친구 공개/비공개 설정 시 선택된 친구들의 userId 배열
 * @type {import('vue').Ref<string[]>}
 */
const selectedUserIds = ref<string[]>([]);

/**
 * 전체 친구 목록 데이터
 * @type {import('vue').Ref<any[]>}
 */
const friendsList = ref<any[]>([]);

/**
 * 친구 선택 UI 표시 여부
 * @type {import('vue').Ref<boolean>}
 */
const showFriendSelector = ref(false);

/**
 * 친구 검색 입력 텍스트
 * @type {import('vue').Ref<string>}
 */
const friendSearchText = ref('');

/**
 * 친구 검색 결과 배열
 * @type {import('vue').Ref<any[]>}
 */
const friendSearchResults = ref<any[]>([]);

/**
 * 친구 검색 입력 필드 포커스 상태
 * @type {import('vue').Ref<boolean>}
 */
const isFriendSearchFocused = ref(false);

/**
 * 현재 로그인한 사용자의 프로필 정보
 * @type {import('vue').Ref<any | null>}
 */
const myProfile = ref<any | null>(null);

/**
 * 친구 검색 디바운싱을 위한 타이머 ID
 * @type {number}
 */
let friendSearchTimeout: number;

// ==================== 리포스트 관련 상태 ====================

/**
 * 리포스트 모드 여부를 확인하는 computed 속성
 * @type {import('vue').ComputedRef<boolean>}
 */
const isRepostMode = computed(() => uiStore.isRepostMode);

/**
 * 리포스트할 원본 게시글 정보
 * @type {import('vue').ComputedRef<any>}
 */
const originalPost = computed(() => uiStore.repostOriginalPost);

/**
 * 원본 게시글의 미디어 파일들의 Blob URL 매핑
 * @type {import('vue').Ref<Record<string, string>>}
 */
const originalPostMediaUrls = ref<Record<string, string>>({});

// ==================== Computed 속성들 ====================

/**
 * 특정 친구 선택이 필요한 공개 옵션인지 확인
 * @type {import('vue').ComputedRef<boolean>}
 */
const needsFriendSelection = computed(() => {
  return ['SelectedUsers', 'UnselectedUsers'].includes(discoveryOption.value);
});

/**
 * 선택된 친구들의 상세 정보를 반환
 * @type {import('vue').ComputedRef<any[]>}
 */
const getSelectedFriends = computed(() => {
  return friendsList.value.filter(friend => selectedUserIds.value.includes(friend.userId));
});

// ==================== 친구 관련 함수들 ====================

/**
 * 현재 사용자의 친구 목록을 서버에서 로드합니다.
 * RightSidebar 컴포넌트와 동일한 방식으로 구현되어 있습니다.
 * 
 * @async
 * @function loadFriends
 * @returns {Promise<void>}
 * 
 * @description
 * 1. 먼저 내 프로필 정보를 확인하고, 없으면 '/api/User/me'에서 가져옵니다.
 * 2. 프로필 정보가 있으면 '/api/Friendship/{userId}' API로 친구 목록을 조회합니다.
 * 3. 성공 시 friendsList에 저장하고, 실패 시 빈 배열로 초기화합니다.
 * 
 * @example
 * await loadFriends();
 * console.log('친구 목록:', friendsList.value);
 */
const loadFriends = async () => {
  try {
    console.log('🔄 친구 목록 로드 시작...');
    
    // 내 프로필 정보가 없으면 먼저 가져오기
    if (!myProfile.value) {
      console.log('📝 내 프로필 정보 가져오는 중...');
      const profileRes = await apiClient.get('/api/User/me');
      myProfile.value = profileRes.data;
      console.log('✅ 내 프로필:', myProfile.value);
    }
    
    if (myProfile.value) {
      console.log('👥 친구 목록 가져오는 중...');
      const response = await apiClient.get(`/api/Friendship/${myProfile.value.userId}`);
      friendsList.value = response.data;
      console.log('✅ 친구 목록 로드 완료:', friendsList.value.length, '명');
      console.log('📋 친구 목록 데이터:', friendsList.value);
    }
  } catch (error) {
    console.error('❌ 친구 목록 로드 실패:', error);
    friendsList.value = [];
  }
};

/**
 * 친구 검색 입력 처리 함수 (디바운싱 적용)
 * 헤더의 검색 기능과 동일한 방식으로 구현되어 있습니다.
 * 
 * @function onFriendSearchInput
 * @returns {void}
 * 
 * @description
 * 1. 이전 검색 타이머를 클리어하여 과도한 API 호출을 방지합니다.
 * 2. 검색어가 비어있으면 결과를 초기화합니다.
 * 3. 300ms 지연 후 검색 API를 호출합니다.
 * 4. 전체 사용자 검색 후 내 친구 목록으로 필터링합니다.
 * 
 * @example
 * // input 이벤트에서 자동 호출됨
 * <input @input="onFriendSearchInput" />
 */
const onFriendSearchInput = () => {
  clearTimeout(friendSearchTimeout);
  if (!friendSearchText.value.trim()) {
    friendSearchResults.value = [];
    return;
  }
  
  console.log('🔍 검색어:', friendSearchText.value);
  
  friendSearchTimeout = window.setTimeout(async () => {
    try {
      console.log('🌐 검색 API 호출 중...');
      const response = await apiClient.get(`/api/User/nickname-search/${friendSearchText.value}`);
      console.log('📨 검색 API 응답:', response.data);
      
      // 검색 결과 중 내 친구만 필터링
      const myFriendIds = new Set(friendsList.value.map(f => f.userId));
      console.log('👤 내 친구 ID 목록:', Array.from(myFriendIds));
      
      friendSearchResults.value = response.data.filter((user: any) => myFriendIds.has(user.userId));
      console.log('✅ 필터링된 검색 결과:', friendSearchResults.value);
      console.log('📊 검색 결과:', response.data.length, '개 중 친구:', friendSearchResults.value.length, '명');
    } catch (error) {
      console.error('❌ 친구 검색 실패:', error);
      friendSearchResults.value = [];
    }
  }, 300);
};

/**
 * 친구 검색 결과에서 사용자를 선택했을 때 처리 함수
 * 
 * @function selectFriendFromSearch
 * @param {any} user - 선택된 사용자 객체 (userId, nickname, handle 등 포함)
 * @returns {void}
 * 
 * @description
 * 1. 선택된 사용자를 친구 선택 목록에 추가/제거합니다.
 * 2. 검색 입력을 초기화하고 결과를 숨깁니다.
 * 3. 검색 필드의 포커스를 해제합니다.
 * 
 * @example
 * selectFriendFromSearch({
 *   userId: '123',
 *   nickname: '홍길동',
 *   handle: 'hong123'
 * });
 */
const selectFriendFromSearch = (user: any) => {
  console.log('✅ 친구 선택:', user);
  toggleFriendSelection(user.userId);
  friendSearchText.value = '';
  friendSearchResults.value = [];
  isFriendSearchFocused.value = false;
};

/**
 * 친구 검색 결과 드롭다운을 숨기는 함수
 * blur 이벤트에서 호출되며, 클릭 이벤트 처리를 위해 지연시간을 둡니다.
 * 
 * @function hideFriendSearchResults
 * @returns {void}
 * 
 * @description
 * 200ms 지연 후 검색 결과와 포커스 상태를 초기화합니다.
 * 이 지연시간은 사용자가 검색 결과를 클릭할 수 있는 시간을 확보합니다.
 */
const hideFriendSearchResults = () => {
  setTimeout(() => { 
    isFriendSearchFocused.value = false;
    friendSearchResults.value = [];
  }, 200);
};

/**
 * 친구 선택/해제 토글 함수
 * 
 * @function toggleFriendSelection
 * @param {string} userId - 토글할 친구의 사용자 ID
 * @returns {void}
 * 
 * @description
 * 1. 이미 선택된 친구라면 선택 목록에서 제거합니다.
 * 2. 선택되지 않은 친구라면 선택 목록에 추가합니다.
 * 3. 변경사항을 콘솔에 로깅합니다.
 * 
 * @example
 * toggleFriendSelection('user123'); // 친구 선택
 * toggleFriendSelection('user123'); // 친구 선택 해제
 */
const toggleFriendSelection = (userId: string) => {
  const index = selectedUserIds.value.indexOf(userId);
  if (index > -1) {
    selectedUserIds.value.splice(index, 1);
  } else {
    selectedUserIds.value.push(userId);
  }
  console.log('👥 선택된 친구들:', selectedUserIds.value);
};

/**
 * 공개 설정 옵션 변경 시 처리 함수
 * 
 * @function onDiscoveryOptionChange
 * @returns {void}
 * 
 * @description
 * 1. 이전에 선택된 친구 목록을 초기화합니다.
 * 2. 특정 친구 선택이 필요한 옵션인지 확인합니다.
 * 3. 필요한 경우 친구 선택 UI를 표시하고 친구 목록을 로드합니다.
 * 4. 불필요한 경우 친구 선택 UI를 숨깁니다.
 * 
 * @example
 * // select 요소의 change 이벤트에서 자동 호출됨
 * <select @change="onDiscoveryOptionChange">
 */
const onDiscoveryOptionChange = () => {
  selectedUserIds.value = [];
  console.log('🔧 공개 설정 변경:', discoveryOption.value);
  if (needsFriendSelection.value) {
    showFriendSelector.value = true;
    loadFriends();
  } else {
    showFriendSelector.value = false;
  }
};

// ==================== 게시글 작성 함수들 ====================

/**
 * 메인 게시글 제출 함수
 * 리포스트 모드와 일반 모드를 구분하여 처리합니다.
 * 
 * @async
 * @function submitPost
 * @returns {Promise<void>}
 * 
 * @description
 * 1. 리포스트 모드인 경우 handleRepost() 함수를 호출합니다.
 * 2. 일반 모드인 경우 새로운 게시글 작성 로직을 수행합니다.
 * 3. 특정 친구 선택 옵션의 경우 2단계 프로세스로 처리합니다:
 *    - 1단계: Friends 옵션으로 게시글 생성
 *    - 2단계: 원하는 옵션으로 공개 설정 변경
 * 
 * @throws {Error} 게시글 작성 실패 시 에러를 throw하고 사용자에게 알림
 * 
 * @example
 * await submitPost();
 */
const submitPost = async () => {
  // 리포스트 모드인 경우 다른 로직 처리
  if (isRepostMode.value && originalPost.value) {
    return await handleRepost();
  }

  if (!newPostText.value.trim() && attachedFiles.value.length === 0 && !attachedLink.value.trim()) {
    alert('내용을 입력해주세요.');
    return;
  }

  try {
    // selectedUserIds를 일반 배열로 변환
    const selectedUserIdsArray = [...selectedUserIds.value];
    console.log('📤 전송할 선택된 친구 ID들:', selectedUserIdsArray);
    
    // 특정 친구 선택이 필요한 경우 일단 Friends로 생성 후 변경
    const isSpecificFriendOption = ['SelectedUsers', 'UnselectedUsers'].includes(discoveryOption.value);
    const initialDiscoveryOption = isSpecificFriendOption ? 'Friends' : discoveryOption.value;
    
    // 게시글 데이터 타입을 명시적으로 정의하여 타입 안전성 확보
    const postDto: {
      DiscoveryOption: string;
      Contents: Array<{
        $type: string;
        Text?: string;
        FileName?: string; 
        Description?: string;
      }>;
      ParentPostId: string | null;
      DiscoveryOptionSelectedUserIds: string[];
    } = {
      DiscoveryOption: initialDiscoveryOption,
      Contents: [],
      ParentPostId: null,
      DiscoveryOptionSelectedUserIds: [] // 일단 빈 배열로 전송
    };

    // 텍스트 내용이 있는 경우 Contents 배열에 추가
    if (newPostText.value.trim()) {
      postDto.Contents.push({ $type: 'text', Text: newPostText.value });
    }

    // 링크가 있는 경우 Contents 배열에 추가
    if (attachedLink.value.trim()) {
      postDto.Contents.push({ $type: 'text', Text: attachedLink.value });
    }

    const formData = new FormData();

    // 첨부 파일들을 FormData에 추가하고 Contents 배열에 메타데이터 추가
    attachedFiles.value.forEach(file => {
      formData.append('Files', file, file.name);
      postDto.Contents.push({
        $type: 'upload',
        FileName: file.name,
        Description: ''
      });
    });

    console.log('📋 1단계 - 게시글 생성용 postDto:', postDto);
    formData.append('JsonData', JSON.stringify(postDto));

    console.log('🚀 1단계 - 게시글 작성 API 호출 중...');
    const createResponse = await apiClient.post('/api/Post', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });

    console.log('✅ 1단계 - 게시글 작성 성공!');
    console.log('📊 응답 데이터 타입:', typeof createResponse.data);
    console.log('📊 응답 데이터 내용:', createResponse.data);
    console.log('📊 응답 헤더:', createResponse.headers);
    console.log('📊 전체 응답 객체:', createResponse);

    // 특정 친구 선택이 필요한 경우 2단계 진행
    if (isSpecificFriendOption && selectedUserIdsArray.length > 0) {
      console.log('🔄 2단계 - 공개 설정 변경 시작...');
      
      // 생성된 게시글의 ID 추출 시도
      let postId;
      
      // 방법 1: 응답 데이터에서 직접 추출
      if (typeof createResponse.data === 'string' && createResponse.data.trim()) {
        postId = createResponse.data.trim();
        console.log('🎯 방법1 - 응답 문자열에서 ID 추출:', postId);
      } else if (createResponse.data?.id) {
        postId = createResponse.data.id;
        console.log('🎯 방법1 - 응답 객체에서 ID 추출:', postId);
      } else if (Array.isArray(createResponse.data) && createResponse.data[0]?.id) {
        postId = createResponse.data[0].id;
        console.log('🎯 방법1 - 응답 배열에서 ID 추출:', postId);
      }

      // 방법 2: 응답 헤더에서 ID 찾기
      if (!postId && createResponse.headers.location) {
        const locationHeader = createResponse.headers.location;
        const idMatch = locationHeader.match(/\/([^\/]+)$/);
        if (idMatch) {
          postId = idMatch[1];
          console.log('🎯 방법2 - Location 헤더에서 ID 추출:', postId);
        }
      }

      // 방법 3: 내 최근 게시글 조회해서 ID 가져오기
      if (!postId) {
        console.log('🔍 방법3 - 최근 게시글 조회로 ID 찾기 시도...');
        try {
          if (!myProfile.value) {
            const profileRes = await apiClient.get('/api/User/me');
            myProfile.value = profileRes.data;
          }
          
          const recentPostsRes = await apiClient.get(`/api/Post/user/${myProfile.value.userId}?limit=1`);
          if (recentPostsRes.data && recentPostsRes.data.length > 0) {
            postId = recentPostsRes.data[0].id;
            console.log('🎯 방법3 - 최근 게시글에서 ID 추출:', postId);
          }
        } catch (recentPostError) {
          console.error('❌ 최근 게시글 조회 실패:', recentPostError);
        }
      }

      if (postId) {
        const discoveryUpdateDto = {
          newDiscoveryOption: discoveryOption.value,
          selectedUserIds: selectedUserIdsArray
        };

        console.log('📋 2단계 - 공개 설정 변경용 데이터:', discoveryUpdateDto);
        console.log('🎯 대상 게시글 ID:', postId);

        await apiClient.put(`/api/Post/${postId}/discovery-option`, discoveryUpdateDto);
        console.log('✅ 2단계 - 공개 설정 변경 성공!');
      } else {
        console.error('❌ 모든 방법으로도 게시글 ID를 찾을 수 없습니다.');
        alert('게시글은 생성되었지만 공개 설정 변경에 실패했습니다. 게시글을 직접 수정해주세요.');
      }
    }

    console.log('🎉 전체 프로세스 완료!');

    // 초기화
    newPostText.value = '';
    attachedFiles.value = [];
    previewItems.value = [];
    attachedLink.value = '';
    selectedUserIds.value = [];
    showFriendSelector.value = false;
    uiStore.closePostEditor();
    emit('post-created');
  } catch (error: any) {
    console.error('❌ 게시글 작성 실패:', error);
    console.error('📊 에러 상세정보:', {
      message: error.message,
      response: error.response?.data,
      status: error.response?.status,
      statusText: error.response?.statusText
    });
    
    if (error.response?.status === 500) {
      alert('서버 내부 오류가 발생했습니다. 잠시 후 다시 시도해주세요.');
    } else if (error.response?.data) {
      alert(`게시글 작성 실패: ${error.response.data}`);
    } else {
      alert('게시글 작성에 실패했습니다.');
    }
  }
};

/**
 * 파일 선택 시 처리하는 함수
 * 
 * @function handleFileChange
 * @param {Event} event - input file 요소의 change 이벤트
 * @returns {void}
 * 
 * @description
 * 1. 선택된 파일들을 attachedFiles 배열에 저장합니다.
 * 2. 각 파일에 대해 미리보기 URL을 생성합니다.
 * 3. 파일 타입에 따라 비디오 여부를 판단합니다.
 * 4. previewItems 배열에 미리보기 정보를 저장합니다.
 * 
 * @example
 * <input type="file" @change="handleFileChange" multiple />
 */
const handleFileChange = (event: Event) => {
  const files = (event.target as HTMLInputElement).files;
  if (!files) return;

  attachedFiles.value = [];
  previewItems.value = [];

  for (let i = 0; i < files.length; i++) {
    const file = files[i];
    attachedFiles.value.push(file);
    previewItems.value.push({
      url: URL.createObjectURL(file),
      isVideo: file.type.startsWith('video/')
    });
  }
};

// ==================== 리포스트 관련 함수들 ====================

/**
 * 원본 게시글의 미디어 파일들을 로드하는 함수
 * 
 * @async
 * @function loadOriginalPostMedia
 * @returns {Promise<void>}
 * 
 * @description
 * 1. 원본 게시글이 없으면 함수를 종료합니다.
 * 2. 게시글의 각 콘텐츠를 순회하며 미디어 타입을 찾습니다.
 * 3. 미디어 파일의 ID로 API를 호출하여 Blob 데이터를 가져옵니다.
 * 4. Blob URL을 생성하여 originalPostMediaUrls에 저장합니다.
 * 
 * @example
 * await loadOriginalPostMedia();
 */
const loadOriginalPostMedia = async () => {
  if (!originalPost.value) return;
  
  for (const content of originalPost.value.contents) {
    if ((content as any).$type === 'media' && ((content as any).mediaId || (content as any).thumbnailMediaId)) {
      const id = (content as any).mediaId || (content as any).thumbnailMediaId;
      try {
        const response = await apiClient.get(`/api/media/${id}`, {
          responseType: 'blob',
        });
        originalPostMediaUrls.value[id] = URL.createObjectURL(response.data);
      } catch (error) {
        console.warn('원본 게시글 미디어 로딩 실패:', id);
      }
    }
  }
};

/**
 * 리포스트 처리를 담당하는 함수
 * 
 * @async
 * @function handleRepost
 * @returns {Promise<void>}
 * 
 * @description
 * 2단계 리포스트 프로세스를 수행합니다:
 * 1. 1단계: 원본 게시글에 대해 리포스트 API 호출
 * 2. 2단계: 추가 내용이 있는 경우 리포스트된 게시글을 수정
 * 
 * 리포스트 ID 찾기 과정:
 * - 원본 게시글의 sharedAndRepostedUsers에서 내가 리포스트한 항목 찾기
 * - 찾지 못한 경우 최신 리포스트로 대체
 * 
 * @throws {Error} 리포스트 실패 시 에러를 throw하고 사용자에게 알림
 * 
 * @example
 * await handleRepost();
 */
const handleRepost = async () => {
  try {
    console.log('🔄 리포스트 처리 시작...');
    
    // 1단계: 먼저 리포스트 API 호출
    console.log('🚀 1단계 - 리포스트 API 호출 중...');
    const repostResponse = await apiClient.post(`/api/Post/${originalPost.value!.id}/repost`);
    console.log('✅ 1단계 - 리포스트 완료:', repostResponse.data);
    
    // 추가 내용이 있는 경우 2단계 진행
    if (newPostText.value.trim() || attachedFiles.value.length > 0 || attachedLink.value.trim()) {
      console.log('🔄 2단계 - 추가 내용으로 게시글 수정 시작...');
      
      // 리포스트된 게시글 ID 찾기 (원본 게시글의 sharedAndRepostedUsers에서 찾기)
      let repostId;
      try {
        // 내 프로필 정보가 없으면 먼저 가져오기
        if (!myProfile.value) {
          const profileRes = await apiClient.get('/api/User/me');
          myProfile.value = profileRes.data;
        }
        
        // 잠시 대기 후 원본 게시글 정보를 다시 조회 (리포스트 정보 업데이트된 후)
        await new Promise(resolve => setTimeout(resolve, 500));
        
        const originalPostRes = await apiClient.get(`/api/Post/${originalPost.value!.id}`);
        console.log('🔍 원본 게시글 최신 정보:', originalPostRes.data);
        
        const sharedAndRepostedUsers = originalPostRes.data.sharedAndRepostedUsers || [];
        console.log('🔍 리포스트한 사용자들:', sharedAndRepostedUsers);
        
        // 내가 리포스트한 항목 찾기
        const myRepost = sharedAndRepostedUsers.find((item: any) => {
          console.log(`🔍 리포스트 항목: userId=${item.user?.userId}, postId=${item.postId}, isRepost=${item.isRepost}`);
          return item.user?.userId === myProfile.value.userId && item.isRepost === true;
        });
        
        if (myRepost) {
          repostId = myRepost.postId;
          console.log('🎯 리포스트 ID 찾음:', repostId);
        } else {
          console.warn('⚠️ sharedAndRepostedUsers에서 내 리포스트를 찾을 수 없습니다.');
          
          // Fallback: 최근 생성된 리포스트 중 가장 최신 것 찾기
          const latestRepost = sharedAndRepostedUsers
            .filter((item: any) => item.user?.userId === myProfile.value.userId && item.isRepost === true)
            .sort((a: any, b: any) => new Date(b.sharedAt).getTime() - new Date(a.sharedAt).getTime())[0];
            
          if (latestRepost) {
            repostId = latestRepost.postId;
            console.log('🎯 최신 리포스트 ID로 대체:', repostId);
          }
        }
      } catch (error) {
        console.error('❌ 리포스트 ID 조회 실패:', error);
      }
      
      if (repostId) {
        // 리포스트 수정용 데이터 타입을 명시적으로 정의
        const updateDto: {
          DiscoveryOption: string;
          Contents: Array<{
            $type: string;
            Text?: string;
            FileName?: string;
            Description?: string;
          }>;
          ParentPostId: string;
          DiscoveryOptionSelectedUserIds: string[];
        } = {
          DiscoveryOption: 'Friends',
          Contents: [],
          ParentPostId: originalPost.value!.id,
          DiscoveryOptionSelectedUserIds: []
        };

        // 추가 텍스트 내용이 있는 경우 Contents 배열에 추가
        if (newPostText.value.trim()) {
          updateDto.Contents.push({ $type: 'text', Text: newPostText.value.trim() });
        }

        // 추가 링크가 있는 경우 Contents 배열에 추가
        if (attachedLink.value.trim()) {
          updateDto.Contents.push({ $type: 'text', Text: attachedLink.value.trim() });
        }

        const formData = new FormData();

        // 추가 파일들을 FormData에 추가하고 Contents 배열에 메타데이터 추가
        attachedFiles.value.forEach(file => {
          formData.append('Files', file, file.name);
          updateDto.Contents.push({
            $type: 'upload',
            FileName: file.name,
            Description: ''
          });
        });

        formData.append('JsonData', JSON.stringify(updateDto));

        console.log('🚀 2단계 - 게시글 내용 업데이트 API 호출 중...');
        await apiClient.put(`/api/Post/${repostId}`, formData, {
          headers: { 'Content-Type': 'multipart/form-data' }
        });
        console.log('✅ 2단계 - 게시글 내용 업데이트 완료!');
      }
    }

    console.log('🎉 리포스트 전체 프로세스 완료!');
    
    // 초기화
    newPostText.value = '';
    attachedFiles.value = [];
    previewItems.value = [];
    attachedLink.value = '';
    selectedUserIds.value = [];
    showFriendSelector.value = false;
    uiStore.closePostEditor();
    emit('post-created');
    
  } catch (error: any) {
    console.error('❌ 리포스트 실패:', error);
    console.error('📊 에러 상세정보:', {
      message: error.message,
      response: error.response?.data,
      status: error.response?.status,
      statusText: error.response?.statusText
    });
    
    if (error.response?.status === 500) {
      alert('서버 내부 오류가 발생했습니다. 잠시 후 다시 시도해주세요.');
    } else if (error.response?.data) {
      alert(`리포스트 실패: ${error.response.data}`);
    } else {
      alert('리포스트에 실패했습니다.');
    }
  }
};

// ==================== 생명주기 및 감시자 ====================

/**
 * 리포스트 모드 감지 시 원본 게시글 미디어를 로딩하는 감시자
 * 
 * @description
 * isRepostMode가 true가 되고 originalPost가 존재할 때
 * 즉시 원본 게시글의 미디어 파일들을 로드합니다.
 */
watch(isRepostMode, (newValue) => {
  if (newValue && originalPost.value) {
    loadOriginalPostMedia();
  }
}, { immediate: true });

</script>

<template>
  <div class="post-card create-post-card">
    <div v-if="!uiStore.isPostEditorOpen" class="compact-view" @click="uiStore.openPostEditor">
      오늘 하루, 기억하고 싶은 순간이 있나요?
    </div>

    <div v-else class="expanded-view">
      <!-- 리포스트 헤더 -->
      <div v-if="isRepostMode" class="repost-header">
        <div class="repost-label">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
            <path d="M23.77 15.67c-.292-.293-.767-.293-1.06 0l-2.22 2.22V7.65c0-2.068-1.683-3.75-3.75-3.75h-5.85c-.414 0-.75.336-.75.75s.336.75.75.75h5.85c1.24 0 2.25 1.01 2.25 2.25v10.24l-2.22-2.22c-.293-.293-.768-.293-1.061 0s-.293.768 0 1.061l3.5 3.5c.145.147.337.22.53.22s.383-.072.53-.22l3.5-3.5c.294-.292.294-.767.001-1.06zM3.5 16.44c.414 0 .75-.336.75-.75V5.44c0-1.24 1.01-2.25 2.25-2.25h5.85c.414 0 .75-.336.75-.75s-.336-.75-.75-.75H6.5c-2.068 0-3.75 1.682-3.75 3.75v10.24L.53 13.22c-.293-.293-.768-.293-1.061 0s-.293.768 0 1.061l3.5 3.5c.145.147.337.22.53.22s.383-.072.53-.22l3.5-3.5c.293-.292.293-.767 0-1.06s-.767-.293-1.06 0L3.5 15.44z"/>
          </svg>
          <span>리포스트</span>
        </div>
      </div>
      
      <textarea v-model="newPostText" class="create-post-input" :placeholder="isRepostMode ? '이 게시글에 대한 생각을 추가해보세요...' : '오늘 하루, 기억하고 싶은 순간이 있나요?'" />

      <div class="create-post-actions">
        <label class="action-btn">
          📷📹 파일 업로드
          <input type="file" accept="image/*" multiple @change="handleFileChange" hidden />
        </label>
        <input v-model="attachedLink" placeholder="🔗 링크 붙여넣기" class="link-input" />
      </div>

      <!-- 리포스트 원본 게시글 미리보기 -->
      <div v-if="isRepostMode && originalPost" class="original-post-preview">
        <div class="original-post-card">
          <div class="original-post-author">
            <img :src="originalPost.user.profileThumbnailMediaId ? `/api/Media/${originalPost.user.profileThumbnailMediaId}` : '/src/assets/images/default_profile_image.jpg'" 
                 class="original-author-avatar">
            <div class="original-author-info">
              <div class="original-author-name">{{ originalPost.user.nickname }}</div>
              <div class="original-post-timestamp">{{ new Date(originalPost.createdAt).toLocaleString() }}</div>
            </div>
          </div>
          
          <div class="original-post-content">
            <div v-for="(content, index) in originalPost.contents" :key="index">
              <p v-if="(content as any).$type === 'text'">{{ (content as any).text }}</p>
              
              <div v-else-if="(content as any).$type === 'media' && ((content as any).mediaId || (content as any).thumbnailMediaId)">
                <template v-if="originalPostMediaUrls[(content as any).mediaId || (content as any).thumbnailMediaId]">
                  <video
                    v-if="(content as any).mimeType && (content as any).mimeType.startsWith('video/')"
                    controls
                    class="original-post-media"
                  >
                    <source :src="originalPostMediaUrls[(content as any).mediaId || (content as any).thumbnailMediaId]" :type="(content as any).mimeType" />
                    브라우저가 video 태그를 지원하지 않습니다.
                  </video>
                  <img
                    v-else
                    :src="originalPostMediaUrls[(content as any).mediaId || (content as any).thumbnailMediaId]"
                    :alt="(content as any).description || '게시물 이미지'"
                    class="original-post-media"
                  />
                </template>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 여러 개 미리보기 -->
      <div v-if="previewItems.length" class="preview-box">
        <div v-for="(item, idx) in previewItems" :key="idx" class="preview-item">
          <img v-if="!item.isVideo" :src="item.url" class="preview-image" />
          <video v-else controls class="preview-video">
            <source :src="item.url" />
            브라우저가 video 태그를 지원하지 않습니다.
          </video>
        </div>
      </div>

      <!-- 친구 선택 UI -->
      <div v-if="showFriendSelector" class="friend-selector-section">
        <div class="friend-selector-header">
          <h4>{{ discoveryOption === 'SelectedUsers' ? '공개할 친구 선택' : '비공개할 친구 선택' }}</h4>
          <button @click="showFriendSelector = false" class="close-btn">×</button>
        </div>
        
        <div class="friend-search-container">
          <input 
            v-model="friendSearchText" 
            @input="onFriendSearchInput"
            @focus="isFriendSearchFocused = true"
            @blur="hideFriendSearchResults"
            placeholder="친구 검색..." 
            class="friend-search-input"
          />
          
          <!-- 친구 검색 결과 드롭다운 -->
          <div v-if="isFriendSearchFocused && friendSearchText" class="friend-search-dropdown">
            <div v-if="friendSearchResults.length === 0" class="no-results">검색 결과가 없습니다.</div>
            <div v-else v-for="user in friendSearchResults" :key="user.userId" 
                 @click="selectFriendFromSearch(user)" class="friend-search-item">
              <img :src="user.profileThumbnailMediaId ? `/api/Media/${user.profileThumbnailMediaId}` : '/src/assets/images/default_profile_image.jpg'" 
                   class="friend-search-avatar">
              <div class="friend-search-info">
                <div class="friend-search-name">{{ user.nickname }}</div>
                <div class="friend-search-handle">@{{ user.handle }}</div>
              </div>
            </div>
          </div>
        </div>

        <!-- 선택된 친구들 표시 -->
        <div v-if="selectedUserIds.length > 0" class="selected-friends-display">
          <div class="selected-friends-header">선택된 친구 ({{ selectedUserIds.length }}명)</div>
          <div class="selected-friends-list">
            <div v-for="friend in getSelectedFriends" :key="friend.userId" class="selected-friend-item">
              <img :src="friend.profileThumbnailMediaId ? `/api/Media/${friend.profileThumbnailMediaId}` : '/src/assets/images/default_profile_image.jpg'" 
                   class="selected-friend-avatar">
              <span class="selected-friend-name">{{ friend.nickname }}</span>
              <button @click="toggleFriendSelection(friend.userId)" class="remove-friend-btn">×</button>
            </div>
          </div>
        </div>
      </div>

      <div class="create-post-footer">
        <div class="privacy-selector">
          <select v-model="discoveryOption" @change="onDiscoveryOptionChange">
            <option value="OnlyMe">나만 보기</option>
            <option value="SelectedUsers">특정 친구 공개</option>
            <option value="UnselectedUsers">특정 친구 비공개</option>
            <option value="Friends">친구 공개</option>
            <option value="FriendsOfFriends">친구의 친구 공개</option>
            <option value="Everyone">전체 공개</option>
          </select>
        </div>
        <div class="submit-buttons">
          <button @click="uiStore.closePostEditor" class="btn-cancel">취소</button>
          <button @click="submitPost" class="btn-submit">올리기</button>
        </div>
      </div>
    </div>
  </div>
</template>

