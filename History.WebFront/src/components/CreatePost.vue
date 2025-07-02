<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useUiStore } from '@/stores/ui';
import apiClient from '@/api';
import { defineEmits } from 'vue';

/**
 * CreatePost 컴포넌트의 props와 emits 정의
 */
const uiStore = useUiStore();
const emit = defineEmits(['post-created']);

// ==================== 반응형 상태 변수들 ====================

const generateContentsFromText = (): Array<any> => {
  const text = newPostText.value;
  const result: Array<any> = [];

  // 닉네임 → userId 매핑
  const nicknameToUserIdMap: Record<string, string> = {};
  friendsList.value.forEach(friend => {
    nicknameToUserIdMap[friend.nickname] = friend.userId;
  });

  let currentIndex = 0;
  const mentionRegex = /@(\S+)/g;
  let match;

  while ((match = mentionRegex.exec(text)) !== null) {
    const mentionStart = match.index;
    const mentionEnd = mentionRegex.lastIndex;
    const nickname = match[1];

    // 이전 일반 텍스트 추가
    if (mentionStart > currentIndex) {
      const beforeText = text.substring(currentIndex, mentionStart);
      result.push({ $type: 'text', Text: beforeText });
    }

    // 해당 닉네임이 친구 목록에 있는 경우에만 MentionContent 추가
    const userId = nicknameToUserIdMap[nickname];
    if (userId) {
      result.push({ $type: 'profile', UserId: userId });
    } else {
      // 못 찾으면 텍스트로 처리
      result.push({ $type: 'text', Text: text.substring(mentionStart, mentionEnd) });
    }

    currentIndex = mentionEnd;
  }

  // 남은 텍스트 추가
  if (currentIndex < text.length) {
    result.push({ $type: 'text', Text: text.substring(currentIndex) });
  }

  return result;
};


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
 * 댓글 허용 범위 설정 옵션
 * @type {import('vue').Ref<string>}
 */
 const commentPermission = ref<string | null>(null); 

/**
 * 다른 사용자의 공유 허용 여부
 * @type {import('vue').Ref<boolean>}
 */
 const disallowShare = ref(false); // 기본값 'false' (공유 허용)

/**
 * 예약 발행 시간 (ISO 8601 형식의 문자열 또는 null)
 * @type {import('vue').Ref<string | null>}
 */
 const reservationTime = ref<string | null>(null);

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
const isShareMode = computed(() => uiStore.isShareMode);

/**
 * 리포스트할 원본 게시글 정보
 * @type {import('vue').ComputedRef<any>}
 */
const originalPostForShare = computed(() => uiStore.shareOriginalPost);

/**
 * 원본 게시글의 미디어 파일들의 Blob URL 매핑
 * @type {import('vue').Ref<Record<string, string>>}
 */
const originalPostMediaUrls = ref<Record<string, string>>({});
/**
 * 원본 게시글 작성자의 프로필 이미지 URL
 * @type {import('vue').Ref<string>}
 */
const originalPostAuthorProfileUrl = ref<string>('');

/**
 * 컴포넌트 확장 상태 (인라인 에디터용)
 * @type {import('vue').Ref<boolean>}
 */
const isExpanded = ref(false);

const openInlineEditor = () => {
  isExpanded.value = true; 
};

/**
 * @멘션 관련 상태
 */
const mentionSearchText = ref('');
const mentionSearchResults = ref<UserResponseDto[]>([]);
const isMentioning = ref(false);
const mentionStartIndex = ref(-1);
const mentionDropdownPosition = ref({ top: 0, left: 0 });
const selectedMentionIndex = ref(-1);

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
 * 미디어 파일의 Blob URL을 가져오는 함수
 * @param mediaId - 미디어 ID
 * @returns 이미지 URL 또는 빈 문자열
 */
const getMediaBlobUrl = async (mediaId: string | null | undefined) => {
  if (!mediaId) return '';
  try {
    const response = await apiClient.get(`/api/Media/${mediaId}`, {
      responseType: 'blob'
    });
    const contentType = response.headers['content-type'];
    if (!contentType.startsWith('image')) return '';
    return URL.createObjectURL(response.data);
  } catch {
    return '';
  }
};

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
      // 첫 번째 친구의 데이터 구조 확인
      if (friendsList.value.length > 0) {
        console.log('🔍 친구 데이터 구조 예시:', friendsList.value[0]);
      }
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
    const postDto = {
      DiscoveryOption: initialDiscoveryOption,
      CommentPermission: commentPermission.value, 
      DisallowShare: disallowShare.value,       
      ReservationTime: reservationTime.value,     
      Contents: [] as any[],
      // ✨ 공유 모드일 경우 ParentPostId 설정
      ParentPostId: isShareMode.value ? originalPostForShare.value.id : null,
      DiscoveryOptionSelectedUserIds: [] as string[]
    };

    // 텍스트 내용이 있는 경우 Contents 배열에 추가
    if (newPostText.value.trim()) {
      const textParts = generateContentsFromText();
      postDto.Contents.push(...textParts);
    }

    // 링크가 있는 경우 externalUrl 콘텐츠로 추가
    if (attachedLink.value.trim()) {
      postDto.Contents.push({ 
        $type: 'externalUrl', 
        SourceUrl: attachedLink.value.trim() 
      });
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
    commentPermission.value = null;
    disallowShare.value = false;
    reservationTime.value = null;
    uiStore.closeEditor();
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
 * 취소 버튼 클릭 시 처리하는 함수
 * 
 * @function handleCancel
 * @returns {void}
 * 
 * @description
 * 1. 모달 모드인 경우 에디터를 닫습니다.
 * 2. 타임라인 인라인 모드인 경우 compact-view로 돌아갑니다.
 * 3. 작성 중인 내용들을 초기화합니다.
 */
 const handleCancel = () => {
  // 모달이 열려있다면 모달을 닫습니다.
  if (uiStore.isEditorOpen) {
    uiStore.closeEditor();
  }
  // 인라인으로 확장되었다면 축소합니다.
  if (isExpanded.value) {
    isExpanded.value = false;
  }
  
  // 작성 중인 내용 초기화
  newPostText.value = '';
  attachedFiles.value = [];
  previewItems.value = [];
  attachedLink.value = '';
  selectedUserIds.value = [];
  showFriendSelector.value = false;
  
  // @멘션 관련 초기화
  isMentioning.value = false;
  mentionSearchText.value = '';
  mentionSearchResults.value = [];
};

/**
 * @멘션 검색 타이머
 */
let mentionSearchTimeout: number | null = null;

/**
 * 텍스트 입력 시 @멘션 감지 및 처리
 * 
 * @function handleTextInput
 * @param {Event} event - input 이벤트
 */
const handleTextInput = (event: Event) => {
  const target = event.target as HTMLTextAreaElement;
  const cursorPosition = target.selectionStart;
  const text = target.value;
  
  // @ 심볼 찾기
  const lastAtSymbol = text.lastIndexOf('@', cursorPosition - 1);
  
  if (lastAtSymbol !== -1 && lastAtSymbol < cursorPosition) {
    const searchText = text.substring(lastAtSymbol + 1, cursorPosition);
    
    // 공백이 있으면 멘션 종료
    if (searchText.includes(' ') || searchText.includes('\n')) {
      isMentioning.value = false;
      mentionSearchResults.value = [];
      return;
    }
    
    // @멘션 시작
    isMentioning.value = true;
    mentionStartIndex.value = lastAtSymbol;
    mentionSearchText.value = searchText;
    
    // 드롭다운 위치 계산
    const textareaRect = target.getBoundingClientRect();
    mentionDropdownPosition.value = {
      top: textareaRect.bottom + 5,
      left: textareaRect.left
    };
    
    // 친구 검색
    searchMentions();
  } else {
    isMentioning.value = false;
    mentionSearchResults.value = [];
  }
};

/**
 * @멘션을 위한 친구 검색
 * 
 * @function searchMentions
 */
const searchMentions = () => {
  if (mentionSearchTimeout) {
    clearTimeout(mentionSearchTimeout);
  }
  
  // 친구 목록이 없으면 먼저 로드
  if (friendsList.value.length === 0) {
    loadFriends().then(() => {
      performMentionSearch();
    });
  } else {
    performMentionSearch();
  }
};

const performMentionSearch = async () => {
  let results: UserResponseDto[] = [];
  
  if (!mentionSearchText.value) {
    // 검색어가 없으면 친구 목록 전체 표시
    results = friendsList.value.slice(0, 5);
  } else {
    // 친구 목록에서 필터링
    const filtered = friendsList.value.filter(friend => 
      friend.nickname.toLowerCase().includes(mentionSearchText.value.toLowerCase()) ||
      friend.handle.toLowerCase().includes(mentionSearchText.value.toLowerCase())
    );
    
    results = filtered.slice(0, 5);
  }
  
  // 각 유저의 프로필 이미지 URL 가져오기
  for (const user of results) {
    if (user.profileThumbnailMediaId || user.ProfileThumbnailMediaId) {
      const mediaId = user.profileThumbnailMediaId || user.ProfileThumbnailMediaId;
      const imageUrl = await getMediaBlobUrl(mediaId);
      // UserResponseDto에 이미지 URL 추가
      (user as any).profileImageUrl = imageUrl || '/src/assets/images/default_profile_image.jpg';
    } else {
      (user as any).profileImageUrl = '/src/assets/images/default_profile_image.jpg';
    }
  }
  
  mentionSearchResults.value = results;
  
  // 검색 결과가 변경되면 선택 인덱스 초기화
  selectedMentionIndex.value = -1;
};

/**
 * @멘션 선택
 * 
 * @function selectMention
 * @param {UserResponseDto} user - 선택된 사용자
 */
const selectMention = (user: UserResponseDto) => {
  const text = newPostText.value;
  const beforeMention = text.substring(0, mentionStartIndex.value);
  const afterCursor = text.substring(mentionStartIndex.value + mentionSearchText.value.length + 1);
  
  newPostText.value = `${beforeMention}@${user.nickname} ${afterCursor}`;
  
  isMentioning.value = false;
  mentionSearchResults.value = [];
  mentionSearchText.value = '';
  selectedMentionIndex.value = -1;
};

/**
 * 키보드 이벤트 처리
 * 
 * @function handleKeyDown
 * @param {KeyboardEvent} event - 키보드 이벤트
 */
const handleKeyDown = (event: KeyboardEvent) => {
  if (!isMentioning.value || mentionSearchResults.value.length === 0) return;
  
  switch (event.key) {
    case 'ArrowDown':
      event.preventDefault();
      selectedMentionIndex.value = Math.min(
        selectedMentionIndex.value + 1,
        mentionSearchResults.value.length - 1
      );
      break;
      
    case 'ArrowUp':
      event.preventDefault();
      selectedMentionIndex.value = Math.max(selectedMentionIndex.value - 1, 0);
      break;
      
    case 'Enter':
      event.preventDefault();
      if (selectedMentionIndex.value >= 0) {
        selectMention(mentionSearchResults.value[selectedMentionIndex.value]);
      }
      break;
      
    case 'Escape':
      event.preventDefault();
      isMentioning.value = false;
      mentionSearchResults.value = [];
      selectedMentionIndex.value = -1;
      break;
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
 * 2. 원본 작성자의 프로필 이미지를 로드합니다.
 * 3. 게시글의 각 콘텐츠를 순회하며 미디어 타입을 찾습니다.
 * 4. 미디어 파일의 ID로 API를 호출하여 Blob 데이터를 가져옵니다.
 * 5. Blob URL을 생성하여 originalPostMediaUrls에 저장합니다.
 * 
 * @example
 * await loadOriginalPostMedia();
 */
const loadOriginalPostMedia = async () => {
  if (!originalPostForShare.value) return;
  
  // 원본 작성자 프로필 이미지 로드
  if (originalPostForShare.value.user?.profileThumbnailMediaId) {
    try {
      const response = await apiClient.get(`/api/media/${originalPostForShare.value.user.profileThumbnailMediaId}`, {
        responseType: 'blob',
      });
      originalPostAuthorProfileUrl.value = URL.createObjectURL(response.data);
    } catch (error) {
      console.warn('원본 작성자 프로필 이미지 로딩 실패');
      originalPostAuthorProfileUrl.value = '/src/assets/images/default_profile_image.jpg';
    }
  } else {
    originalPostAuthorProfileUrl.value = '/src/assets/images/default_profile_image.jpg';
  }
  
  // 원본 게시글 미디어 로드
  for (const content of originalPostForShare.value.contents) {
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

// ==================== 생명주기 및 감시자 ====================

/**
 * 리포스트 모드 감지 시 원본 게시글 미디어를 로딩하는 감시자
 * 
 * @description
 * isRepostMode가 true가 되고 originalPost가 존재할 때
 * 즉시 원본 게시글의 미디어 파일들을 로드합니다.
 */
 watch(isShareMode, (newValue) => { 
  if (newValue && originalPostForShare.value) { 
    loadOriginalPostMedia();
  }
}, { immediate: true });

</script>

<template>
  <div class="post-card create-post-card">
    <div v-if="!uiStore.isEditorOpen && !isExpanded" class="compact-view" @click="openInlineEditor">
      <textarea readonly placeholder="오늘 하루, 기억하고 싶은 순간이 있나요?"></textarea>
    </div>

    <div v-else class="expanded-view">
      <div v-if="isShareMode" class="repost-header">
        <div class="repost-label">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 22v-9m-4 4 4-4 4 4m-8-4a9 9 0 1 1 18 0 9 9 0 0 1-18 0Z"/>
          </svg>
          <span>공유하기</span>
        </div>
      </div>
      
      <textarea 
        v-model="newPostText" 
        class="create-post-input" 
        :placeholder="isShareMode ? '공유할 게시글에 생각을 추가해보세요...' : '오늘 하루, 기억하고 싶은 순간이 있나요?'" 
        aria-label="게시글 내용 입력"
      ></textarea>
      
      <div 
        v-if="isMentioning"
        class="mention-dropdown"
        role="listbox"
        :aria-label="`친구 검색 결과: ${mentionSearchResults.length}명`"
        :style="{
          position: 'fixed',
          top: mentionDropdownPosition.top + 'px',
          left: mentionDropdownPosition.left + 'px',
          zIndex: 1000
        }"
      >
        <div v-if="mentionSearchResults.length === 0" class="mention-no-results" role="status">
          {{ friendsList.length === 0 ? '친구가 없습니다' : '검색 결과가 없습니다' }}
        </div>
        <div 
          v-else
          v-for="(user, index) in mentionSearchResults" 
          :key="user.userId"
          class="mention-item"
          :class="{ 'selected': index === selectedMentionIndex }"
          @click="selectMention(user)"
          @mouseenter="selectedMentionIndex = index"
          role="option"
          :aria-selected="index === selectedMentionIndex"
          :aria-label="`${user.nickname} @${user.handle}`"
        >
          <img :src="(user as any).profileImageUrl || '/src/assets/images/default_profile_image.jpg'" :alt="`${user.nickname} 프로필 이미지`">
          <div>
            <div class="nickname">{{ user.nickname }}</div>
            <div class="handle">@{{ user.handle }}</div>
          </div>
        </div>
      </div>
      
      <span id="mention-hint" class="sr-only">
        @ 심볼을 입력하여 친구를 멘션할 수 있습니다. 위아래 화살표로 선택하고 Enter로 확정하세요.
      </span>

      <div class="create-post-actions">
      <label class="action-btn">
        📷📹 파일 업로드
        <input type="file" accept="image/*" multiple @change="handleFileChange" hidden />
      </label>
      <input v-model="attachedLink" placeholder="🔗 링크 붙여넣기" class="link-input" />
    </div>

    <div v-if="isShareMode && originalPostForShare" class="original-post-preview">
        <div class="original-post-card">
          <div class="original-post-author">
            <img :src="originalPostAuthorProfileUrl || '/src/assets/images/default_profile_image.jpg'" 
                 class="original-author-avatar">
            <div class="original-author-info">
              <div class="original-author-name">{{ originalPostForShare.user.nickname }}</div>
              <div class="original-post-timestamp">{{ new Date(originalPostForShare.createdAt).toLocaleString() }}</div>
            </div>
          </div>
          
          <div class="original-post-content">
            <div v-for="(content, index) in originalPostForShare.contents" :key="index">
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

      <div v-if="previewItems.length" class="preview-box">
        <div v-for="(item, idx) in previewItems" :key="idx" class="preview-item">
          <img v-if="!item.isVideo" :src="item.url" class="preview-image" />
          <video v-else controls class="preview-video">
            <source :src="item.url" />
            브라우저가 video 태그를 지원하지 않습니다.
          </video>
        </div>
      </div>

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
        <div class="footer-options-group">
          <div class="option-item">
            <label for="discovery-option">공개</label>
            <select id="discovery-option" v-model="discoveryOption" @change="onDiscoveryOptionChange">
              <option value="OnlyMe">나만 보기</option>
              <option value="SelectedUsers">특정 친구 공개</option>
              <option value="UnselectedUsers">특정 친구 비공개</option>
              <option value="Friends">친구 공개</option>
              <option value="FriendsOfFriends">친구의 친구까지</option>
              <option value="Everyone">전체 공개</option>
            </select>
          </div>
          <div class="option-item">
            <label for="comment-permission">댓글</label>
            <select id="comment-permission" v-model="commentPermission">
              <option :value="null">게시글 설정 따름</option>
              <option value="OnlyMe">나만</option>
              <option value="Friends">친구만</option>
              <option value="FriendsOfFriends">친구의 친구까지</option>
              <option value="Everyone">모든 사람</option>
            </select>
          </div>
          <div class="option-item checkbox-item">
            <input type="checkbox" id="disallow-share" v-model="disallowShare">
            <label for="disallow-share">공유 금지</label>
          </div>
          <div class="option-item">
            <label for="reservation-time">예약</label>
            <input type="datetime-local" id="reservation-time" v-model="reservationTime">
          </div>
        </div>
        <div class="submit-buttons">
          <button @click="handleCancel" class="btn-cancel">취소</button>
          <button @click="submitPost" class="btn-submit">올리기</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>

.post-card {
  background: white;
  border-radius: 8px;
  border: 1px solid #ddd;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.create-post-card { padding: 20px; }

.compact-view textarea {
  width: 100%;
  padding: 12px;
  font-size: 1rem;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  background-color: #f8f9fa;
  cursor: pointer;
  resize: none;
  transition: border-color 0.2s;
  box-sizing: border-box;
}

.create-post-input {
  width: 100%;
  min-height: 100px;
  border: none;
  resize: vertical;
  font-size: 1.1rem;
  padding: 8px 0;
  line-height: 1.5;
}

.create-post-input:focus { outline: none; }

.create-post-actions {
  display: flex;
  gap: 8px;
  padding: 12px 0;
  border-top: 1px solid #eee;
  border-bottom: 1px solid #eee;
  margin: 12px 0;
}

.create-post-actions .action-btn {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 20px;
  background-color: transparent;
  cursor: pointer;
  transition: all 0.2s;
  font-size: 0.9rem;
}

.create-post-actions .action-btn:hover {
  background-color: #f8f9fa;
  border-color: #ed664d;
}

.create-post-footer {
  display: flex;
  justify-content: space-between;
  align-items: flex-end; 
  flex-wrap: wrap; 
  gap: 16px;
  margin-top: 12px;
}

.footer-options-group {
  display: flex;
  flex-wrap: wrap;
  gap: 24px; /* 그룹 간 간격 */
  align-items: flex-end;
}

.privacy-selector select {
  padding: 8px 12px;
  border-radius: 6px;
  border: 1px solid #ddd;
  background-color: white;
  font-size: 0.9rem;
}

.privacy-selector,
.advanced-options {
  display: flex;
  flex-wrap: wrap;
  gap: 16px; 
  align-items: center;
}

.advanced-options select,
.advanced-options input[type="datetime-local"] {
  padding: 8px 12px;
  border-radius: 6px;
  border: 1px solid #ddd;
  background-color: white;
  font-size: 0.9rem;
  height: 38px; 
}

.advanced-options input[type="checkbox"] {
  width: 16px;
  height: 16px;
  cursor: pointer;
}

.advanced-options .checkbox-item label {
  cursor: pointer;
}

.advanced-options .option-item {
  display: flex;
  align-items: center;
  gap: 6px;
}

.advanced-options label {
  font-size: 0.9rem;
  font-weight: 500;
  color: #495057;
}

.submit-buttons { display: flex; gap: 8px; }

.btn-cancel, .btn-submit {
  padding: 8px 24px;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  border: 1px solid transparent;
  transition: all 0.2s;
}

.btn-cancel { 
  background-color: #e9ecef; 
  color: #495057; 
}

.btn-cancel:hover {
  background-color: #dee2e6;
}

.btn-submit { 
  background-color: #ed664d; 
  color: white; 
}

.btn-submit:hover {
  background-color: #e55a47;
}

.link-input {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 0.9rem;
  min-width: 200px;
  transition: border-color 0.2s;
}

.link-input:focus {
  outline: none;
  border-color: #ed664d;
}

/* 미리보기 */
.preview-box {
  margin-top: 8px;
  max-height: 400px;
  overflow: hidden;
  border-radius: 6px;
}

.preview-image, .preview-video {
  width: 100%;
  max-height: 400px;
  border-radius: 6px;
  object-fit: contain;
}

.preview-item {
  margin-bottom: 8px;
}

/* 친구 선택 UI */
.friend-selector-section {
  background-color: #f8f9fa;
  border-radius: 8px;
  padding: 16px;
  margin: 16px 0;
  border: 1px solid #e9ecef;
}

.friend-selector-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.friend-selector-header h4 {
  margin: 0;
  font-size: 1rem;
  font-weight: 600;
  color: #495057;
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.2rem;
  cursor: pointer;
  color: #6c757d;
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  transition: background-color 0.2s;
}

.close-btn:hover {
  background-color: #e9ecef;
}

.friend-search-container {
  position: relative;
  margin-bottom: 16px;
}

.friend-search-input {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 6px;
  font-size: 0.9rem;
  background-color: white;
  transition: border-color 0.2s;
}

.friend-search-input:focus {
  outline: none;
  border-color: #ed664d;
}

.friend-search-dropdown {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  right: 0;
  background: white;
  border-radius: 6px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
  max-height: 200px;
  overflow-y: auto;
  z-index: 1000;
  border: 1px solid #e9ecef;
}

.friend-search-item {
  display: flex;
  align-items: center;
  padding: 10px 12px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.friend-search-item:hover {
  background-color: #f8f9fa;
}

.friend-search-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  margin-right: 10px;
  object-fit: cover;
}

.friend-search-info {
  flex: 1;
}

.friend-search-name {
  font-weight: 500;
  font-size: 0.9rem;
  margin-bottom: 2px;
}

.friend-search-handle {
  font-size: 0.8rem;
  color: #6c757d;
}

.no-results {
  padding: 16px;
  text-align: center;
  color: #6c757d;
  font-size: 0.9rem;
}

.selected-friends-display {
  background-color: white;
  border-radius: 6px;
  padding: 12px;
  border: 1px solid #e9ecef;
}

.selected-friends-header {
  font-weight: 600;
  font-size: 0.9rem;
  color: #495057;
  margin-bottom: 8px;
}

.selected-friends-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.selected-friend-item {
  display: flex;
  align-items: center;
  background-color: #e3f2fd;
  border: 1px solid #2196f3;
  border-radius: 16px;
  padding: 4px 8px 4px 4px;
  font-size: 0.85rem;
}

.selected-friend-avatar {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  margin-right: 6px;
  object-fit: cover;
}

.selected-friend-name {
  font-weight: 500;
  margin-right: 6px;
}

.remove-friend-btn {
  background: none;
  border: none;
  color: #2196f3;
  cursor: pointer;
  font-size: 1rem;
  width: 16px;
  height: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  transition: background-color 0.2s;
}

.remove-friend-btn:hover {
  background-color: rgba(33, 150, 243, 0.1);
}

/* 리포스트 관련 스타일 */
.repost-header {
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid #e9ecef;
}

.repost-label {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #6c757d;
  font-size: 0.9rem;
  font-weight: 500;
}

.repost-label svg {
  color: #6c757d;
}

.original-post-preview {
  margin: 16px 0;
}

.original-post-card {
  border: 1px solid #e9ecef;
  border-radius: 8px;
  padding: 16px;
  background-color: #f8f9fa;
  transition: all 0.2s;
}

.original-post-card:hover {
  border-color: #ced4da;
  background-color: #f1f3f4;
}

.original-post-author {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
}

.original-author-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  object-fit: cover;
}

.original-author-info {
  flex: 1;
}

.original-author-name {
  font-weight: 600;
  font-size: 0.9rem;
  color: #212529;
}

.original-post-timestamp {
  font-size: 0.8rem;
  color: #6c757d;
  margin-top: 2px;
}

.original-post-content {
  color: #495057;
  line-height: 1.5;
}

.original-post-content p {
  margin: 0 0 8px 0;
}

.original-post-media {
  max-width: 100%;
  max-height: 200px;
  border-radius: 6px;
  object-fit: contain;
  margin-top: 8px;
}

/* @멘션 드롭다운 스타일 */
.mention-dropdown {
  background: white;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  max-height: 200px;
  overflow-y: auto;
  width: 250px;
}

.mention-item {
  display: flex;
  align-items: center;
  padding: 8px 12px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.mention-item:hover,
.mention-item.selected {
  background-color: #f5f5f5;
}

.mention-item.selected {
  background-color: #e8f5ff;
}

.mention-no-results {
  padding: 12px;
  text-align: center;
  color: #666;
  font-size: 14px;
}

.mention-item img {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  margin-right: 8px;
  object-fit: cover;
}

.mention-item .nickname {
  font-weight: 500;
  color: #333;
  font-size: 14px;
}

.mention-item .handle {
  color: #666;
  font-size: 12px;
}

/* 스크린 리더 전용 텍스트 */
.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}
</style>