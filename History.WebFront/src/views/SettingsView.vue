<script setup lang="ts">
import { ref, onMounted } from 'vue'
import apiClient from '@/api'
import { useRouter, RouterLink } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import type { UserResponseDto } from '@/types'

const router = useRouter()
const authStore = useAuthStore()

const birthdayText = ref('설정되지 않음')
const pushNotifications = ref({
  comment: 'OnlyMe',
  commentMention: 'OnlyMe', 
  commentLike: 'OnlyMe',
  postReaction: 'OnlyMe',
  postMention: 'OnlyMe',
  favoriteFriendNewPost: 'OnlyMe'
})
const friendDiscovery = ref('전체공개')
const blockedUsers = ref<UserResponseDto[]>([])
const ignoredUsers = ref<UserResponseDto[]>([])

// 알림 권한 옵션들
const notificationOptions = [
  { value: 'Everyone', label: '모든 사람' },
  { value: 'FriendsOfFriends', label: '친구의 친구까지' },
  { value: 'Friends', label: '친구만' },
  { value: 'OnlyMe', label: '꺼짐' }
]

const favoriteFriendOptions = [
  { value: 'Everyone', label: '켜짐' },
  { value: 'OnlyMe', label: '꺼짐' }
]

/**
 * 사용자 설정 데이터를 서버에서 가져와 화면에 표시하는 함수
 * 
 * 이 함수는 다음과 같은 설정 정보들을 로드합니다:
 * - 생일 정보
 * - 6가지 푸시 알림 권한 설정
 * - 친구 목록 공개 범위 설정
 * - 차단된 사용자 목록
 * - 무시된 사용자 목록
 * 
 * @async
 * @function fetchSettingsData
 * @throws {Error} API 호출 실패 시 콘솔에 에러 로그 출력
 * 
 * @example
 * // 컴포넌트 마운트 시 자동 호출
 * onMounted(fetchSettingsData)
 */
const fetchSettingsData = async () => {
  try {
    console.log('[설정 데이터 로딩 시작]')
    
    const userRes = await apiClient.get('/api/User/me')
    const user = userRes.data
    
    console.log('[사용자 정보]', user)
    
    birthdayText.value = user.birthday || '설정되지 않음'
    
    // 푸시 알림 설정 확인
    console.log('[푸시 알림 설정 확인]', {
      commentPushNotificationPermission: user.commentPushNotificationPermission,
      commentMentionPushNotificationPermission: user.commentMentionPushNotificationPermission,
      commentLikePushNotificationPermission: user.commentLikePushNotificationPermission,
      postReactionPushNotificationPermission: user.postReactionPushNotificationPermission,
      postMentionPushNotificationPermission: user.postMentionPushNotificationPermission,
      isFavoriteFriendNewPostPushNotificationEnabled: user.isFavoriteFriendNewPostPushNotificationEnabled
    })
    
    // 각 알림 타입별로 설정값 적용
    pushNotifications.value.comment = user.commentPushNotificationPermission || 'OnlyMe'
    pushNotifications.value.commentMention = user.commentMentionPushNotificationPermission || 'OnlyMe' 
    pushNotifications.value.commentLike = user.commentLikePushNotificationPermission || 'OnlyMe'
    pushNotifications.value.postReaction = user.postReactionPushNotificationPermission || 'OnlyMe'
    pushNotifications.value.postMention = user.postMentionPushNotificationPermission || 'OnlyMe'
    pushNotifications.value.favoriteFriendNewPost = user.isFavoriteFriendNewPostPushNotificationEnabled ? 'Everyone' : 'OnlyMe'
    
    console.log('[설정된 푸시 상태]', pushNotifications.value)
    
    friendDiscovery.value = user.friendDiscovery || '전체공개'

    const [blocked, ignored] = await Promise.all([
      apiClient.get('/api/Friendship/blocked'),
      apiClient.get('/api/Friendship/ignored'),
    ])
    blockedUsers.value = blocked.data
    ignoredUsers.value = ignored.data
    
    console.log('[설정 데이터 로딩 완료]')
  } catch (err: any) {
    console.error('설정 데이터 불러오기 실패:', err)
    console.error('에러 상세:', err.response?.data)
  }
}

/**
 * 사용자의 생일을 설정하거나 변경하는 함수
 * 
 * 이 함수는 prompt를 사용하여 사용자로부터 생일 정보를 입력받고,
 * 서버에 업데이트 요청을 보냅니다.
 * 
 * 입력 형식:
 * - 월: 1~12 사이의 숫자
 * - 일: 1~31 사이의 숫자
 * 
 * @async
 * @function editBirthday
 * 
 * @example
 * // 생일 변경 버튼 클릭 시 호출
 * <button @click="editBirthday">생일 변경</button>
 */
const editBirthday = async () => {
  const month = prompt('몇 월생인가요? (1~12 숫자 입력)')
  const day = prompt('며칠인가요? (1~31 숫자 입력)')
  if (!month || !day) return
  const dateStr = `${month}-${day}`
  try {
    await apiClient.put('/api/User/birthday', { birthday: dateStr })
    birthdayText.value = `${month}월 ${day}일`
    alert('생일이 설정되었습니다.')
  } catch {
    alert('생일 설정에 실패했습니다.')
  }
}

/**
 * 관심 친구의 새 게시물 알림을 토글하는 함수
 * 
 * 이 함수는 FavoriteFriendNewPost 알림 타입만을 처리하며,
 * 다른 일반 알림들과 달리 단순한 켜짐/꺼짐 토글 방식을 사용합니다.
 * 
 * 동작 방식:
 * - 현재 'Everyone'이면 'OnlyMe'로 변경 (끄기)
 * - 현재 'OnlyMe'이면 'Everyone'으로 변경 (켜기)
 * 
 * @async
 * @function setPush
 * @param {'favoriteFriendNewPost'} type - 토글할 알림 타입 (현재는 관심친구 알림만 지원)
 * 
 * @example
 * // 토글 스위치 클릭 시 호출
 * <input @change="setPush('favoriteFriendNewPost')" />
 */
const setPush = async (type: 'favoriteFriendNewPost') => {
  if (type !== 'favoriteFriendNewPost') {
    console.warn('setPush는 favoriteFriendNewPost만 지원합니다.')
    return
  }
  
  const currentValue = pushNotifications.value[type]
  const newValue = currentValue === 'Everyone' ? 'OnlyMe' : 'Everyone'
  
  console.log(`[${type} 알림 설정 변경]`, {
    currentValue,
    newValue
  })
  
  try {
    const response = await apiClient.put('/api/User/push-notification-permission', {
      type: 'FavoriteFriendNewPost',
      accessPermission: newValue
    })
    
    console.log(`[${type} 알림 설정 성공]`, response.data)
    pushNotifications.value[type] = newValue
    
    const statusText = newValue === 'Everyone' ? '켜짐' : '꺼짐'
    console.log(`관심 친구의 새 게시물 알림이 ${statusText}으로 설정되었습니다.`)
    
  } catch (err: any) {
    console.error(`[${type} 알림 설정 실패]`, err)
    console.error('에러 상세:', err.response?.data)
    
    // 실패시 상태 롤백
    pushNotifications.value[type] = currentValue
    
    const errorMsg = err.response?.data?.title || '알림 설정에 실패했습니다.'
    alert(`알림 설정 실패: ${errorMsg}`)
  }
}

/**
 * 일반 푸시 알림 권한을 업데이트하는 함수 (드롭다운 선택용)
 * 
 * 이 함수는 5가지 일반 알림 타입의 권한을 변경할 때 사용됩니다:
 * - Comment: 댓글 알림
 * - CommentMention: 댓글 언급 알림
 * - CommentLike: 댓글 좋아요 알림
 * - PostReaction: 게시글 반응 알림
 * - PostMention: 게시글 언급 알림
 * 
 * 권한 레벨: Everyone > FriendsOfFriends > Friends > OnlyMe
 * 
 * @async
 * @function updateNotificationPermission
 * @param {string} type - API에서 사용하는 알림 타입 (Comment, CommentMention 등)
 * @param {string} permission - 설정할 권한 레벨 (Everyone, FriendsOfFriends, Friends, OnlyMe)
 * 
 * @example
 * // 댓글 알림을 친구만 받도록 설정
 * updateNotificationPermission('Comment', 'Friends')
 * 
 * // 언급 알림을 모든 사람에게서 받도록 설정
 * updateNotificationPermission('CommentMention', 'Everyone')
 */
// 일반 알림 설정 변경 (드롭다운)
const updateNotificationPermission = async (type: string, permission: string) => {
  console.log(`[${type} 알림 설정 변경]`, { type, permission })
  
  // API 타입과 상태 키 매핑
  const typeToKeyMap: Record<string, keyof typeof pushNotifications.value> = {
    'Comment': 'comment',
    'CommentMention': 'commentMention',
    'CommentLike': 'commentLike',
    'PostReaction': 'postReaction',
    'PostMention': 'postMention'
  }
  
  const stateKey = typeToKeyMap[type]
  if (!stateKey) {
    console.error('지원하지 않는 알림 타입:', type)
    return
  }
  
  // 현재 상태 저장 (실패 시 롤백용)
  const previousValue = pushNotifications.value[stateKey]
  
  try {
    const response = await apiClient.put('/api/User/push-notification-permission', {
      type: type,
      accessPermission: permission
    })
    
    console.log(`[${type} 알림 설정 성공]`, response.data)
    
    const statusText = permission === 'OnlyMe' ? '꺼짐' : '켜짐'
    console.log(`${type} 알림이 ${statusText}으로 설정되었습니다.`)
    
  } catch (err: any) {
    console.error(`[${type} 알림 설정 실패]`, err)
    console.error('에러 상세:', err.response?.data)
    
    // 실패 시 이전 값으로 롤백
    pushNotifications.value[stateKey] = previousValue
    
    const errorMsg = err.response?.data?.title || '알림 설정에 실패했습니다.'
    alert(`알림 설정 실패: ${errorMsg}`)
  }
}

/**
 * 친구 목록 공개 범위를 변경하는 함수
 * 
 * 이 함수는 다른 사용자들이 내 친구 목록을 볼 수 있는 범위를 설정합니다.
 * 
 * 공개 범위 옵션:
 * - 전체공개: 모든 사용자가 내 친구 목록을 볼 수 있음
 * - 친구만: 내 친구들만 내 친구 목록을 볼 수 있음  
 * - 비공개: 나만 내 친구 목록을 볼 수 있음
 * 
 * @async
 * @function editFriendDiscovery
 * 
 * @example
 * // 공개 범위 변경 버튼 클릭 시 호출
 * <button @click="editFriendDiscovery">공개 범위 변경</button>
 */
const editFriendDiscovery = async () => {
  const options = ['전체공개', '친구만', '비공개']
  const selection = prompt('공개 범위를 선택하세요 (전체공개, 친구만, 비공개)')
  if (selection && options.includes(selection)) {
    try {
      await apiClient.put('/api/User/friend-discovery', { value: selection })
      friendDiscovery.value = selection
      alert('공개 범위가 변경되었습니다.')
    } catch {
      alert('공개 범위 변경 실패')
    }
  }
}

/**
 * 현재 세션에서 로그아웃하고 로그인 페이지로 이동하는 함수
 * 
 * 이 함수는 사용자 인증 토큰을 삭제하고 로그인 페이지로 리다이렉트합니다.
 * 
 * @function logout
 * 
 * @example
 * // 로그아웃 버튼 클릭 시 호출
 * <button @click="logout">로그아웃</button>
 */
const logout = () => {
  authStore.logout()
  router.push('/login')
}

/**
 * 회원 탈퇴를 처리하는 함수
 * 
 * 이 함수는 사용자 계정을 영구적으로 삭제합니다.
 * 안전을 위해 확인 문구("탈퇴하겠습니다")를 정확히 입력해야 실행됩니다.
 * 
 * 주의사항:
 * - 모든 사용자 데이터가 삭제됩니다
 * - 복구가 불가능합니다
 * - 탈퇴 완료 후 자동으로 로그아웃됩니다
 * 
 * @async
 * @function withdraw
 * 
 * @example
 * // 회원 탈퇴 버튼 클릭 시 호출
 * <button @click="withdraw">회원 탈퇴</button>
 */
const withdraw = async () => {
  const confirmText = prompt('정말로 탈퇴하시려면 "탈퇴하겠습니다"를 입력하세요')
  if (confirmText === '탈퇴하겠습니다') {
    try {
      await apiClient.delete('/api/User')
      alert('회원 탈퇴가 완료되었습니다.')
      logout()
    } catch {
      alert('회원 탈퇴 실패')
    }
  }
}

/**
 * 특정 사용자의 차단을 해제하는 함수
 * 
 * 이 함수는 차단된 사용자 목록에서 특정 사용자를 제거하고,
 * UI에서도 해당 사용자를 즉시 제거합니다.
 * 
 * @async
 * @function unblockUser
 * @param {string} userId - 차단을 해제할 사용자의 ID
 * 
 * @example
 * // 차단 해제 버튼 클릭 시 호출
 * <button @click="unblockUser(user.userId)">차단 해제</button>
 */
const unblockUser = async (userId: string) => {
  try {
    await apiClient.delete(`/api/Friendship/block/${userId}`)
    blockedUsers.value = blockedUsers.value.filter(u => u.userId !== userId)
    alert('차단을 해제했습니다.')
  } catch {
    alert('차단 해제 실패')
  }
}

/**
 * 특정 사용자의 무시를 해제하는 함수
 * 
 * 이 함수는 무시된 사용자 목록에서 특정 사용자를 제거하고,
 * UI에서도 해당 사용자를 즉시 제거합니다.
 * 
 * @async
 * @function unignoreUser
 * @param {string} userId - 무시를 해제할 사용자의 ID
 * 
 * @example
 * // 무시 해제 버튼 클릭 시 호출
 * <button @click="unignoreUser(user.userId)">무시 해제</button>
 */
const unignoreUser = async (userId: string) => {
  try {
    await apiClient.delete(`/api/Friendship/ignore/${userId}`)
    ignoredUsers.value = ignoredUsers.value.filter(u => u.userId !== userId)
    alert('무시를 해제했습니다.')
  } catch {
    alert('무시 해제 실패')
  }
}

onMounted(fetchSettingsData)
</script>

<template>
  <div class="settings-layout">
    <main class="main-content">
      <div class="settings-container">
        <div class="page-header">
          <h1>⚙️ 설정</h1>
          <p class="page-subtitle">서브헤더를 적어주세요!</p>
        </div>

        <!-- 개인정보 설정 -->
        <div class="settings-card">
          <div class="card-header">
            <h2>🎂 개인정보</h2>
          </div>
          <div class="setting-item">
            <div class="setting-info">
              <label class="setting-label">생일</label>
              <span class="setting-value">{{ birthdayText }}</span>
            </div>
            <button @click="editBirthday" class="btn btn-secondary">변경</button>
          </div>
        </div>

        <!-- 알림 설정 -->
        <div class="settings-card">
          <div class="card-header">
            <h2>🔔 알림 설정</h2>
            <p class="card-description">받고 싶은 알림을 선택하세요</p>
          </div>
          <div class="setting-item">
            <div class="setting-info">
              <label class="setting-label">댓글 푸시 알림</label>
              <span class="setting-description">다른 사용자가 내 게시물에 댓글을 달았을 때 알림을 받습니다</span>
            </div>
            <select 
              v-model="pushNotifications.comment" 
              @change="updateNotificationPermission('Comment', pushNotifications.comment)"
              class="notification-select"
            >
              <option v-for="option in notificationOptions" :key="option.value" :value="option.value">
                {{ option.label }}
              </option>
            </select>
          </div>
          <div class="setting-item">
            <div class="setting-info">
              <label class="setting-label">댓글 언급 푸시 알림</label>
              <span class="setting-description">다른 사용자가 댓글에서 나를 언급했을 때 알림을 받습니다</span>
            </div>
            <select 
              v-model="pushNotifications.commentMention" 
              @change="updateNotificationPermission('CommentMention', pushNotifications.commentMention)"
              class="notification-select"
            >
              <option v-for="option in notificationOptions" :key="option.value" :value="option.value">
                {{ option.label }}
              </option>
            </select>
          </div>
          <div class="setting-item">
            <div class="setting-info">
              <label class="setting-label">댓글 좋아요 푸시 알림</label>
              <span class="setting-description">다른 사용자가 내 댓글에 좋아요를 눌렀을 때 알림을 받습니다</span>
            </div>
            <select 
              v-model="pushNotifications.commentLike" 
              @change="updateNotificationPermission('CommentLike', pushNotifications.commentLike)"
              class="notification-select"
            >
              <option v-for="option in notificationOptions" :key="option.value" :value="option.value">
                {{ option.label }}
              </option>
            </select>
          </div>
          <div class="setting-item">
            <div class="setting-info">
              <label class="setting-label">게시글 좋아요 푸시 알림</label>
              <span class="setting-description">다른 사용자가 내 게시물에 반응을 했을 때 알림을 받습니다</span>
            </div>
            <select 
              v-model="pushNotifications.postReaction" 
              @change="updateNotificationPermission('PostReaction', pushNotifications.postReaction)"
              class="notification-select"
            >
              <option v-for="option in notificationOptions" :key="option.value" :value="option.value">
                {{ option.label }}
              </option>
            </select>
          </div>
          <div class="setting-item">
            <div class="setting-info">
              <label class="setting-label">게시글 언급 푸시 알림</label>
              <span class="setting-description">다른 사용자가 게시물에서 나를 언급했을 때 알림을 받습니다</span>
            </div>
            <select 
              v-model="pushNotifications.postMention" 
              @change="updateNotificationPermission('PostMention', pushNotifications.postMention)"
              class="notification-select"
            >
              <option v-for="option in notificationOptions" :key="option.value" :value="option.value">
                {{ option.label }}
              </option>
            </select>
          </div>
          <div class="setting-item">
            <div class="setting-info">
              <label class="setting-label">관심 친구의 새 게시글 푸시 알림</label>
              <span class="setting-description">관심 친구가 새로운 게시물을 작성했을 때 알림을 받습니다</span>
            </div>
            <div class="toggle-container">
              <label class="toggle-switch">
                <input 
                  type="checkbox" 
                  :checked="pushNotifications.favoriteFriendNewPost === 'Everyone'" 
                  @change="setPush('favoriteFriendNewPost')"
                />
                <span class="toggle-slider"></span>
              </label>
              <span class="toggle-label" :class="{ active: pushNotifications.favoriteFriendNewPost === 'Everyone' }">
                {{ pushNotifications.favoriteFriendNewPost === 'Everyone' ? '켜짐' : '꺼짐' }}
              </span>
            </div>
          </div>
        </div>

        <!-- 프라이버시 설정 -->
        <div class="settings-card">
          <div class="card-header">
            <h2>👥 프라이버시</h2>
            <p class="card-description">다른 사용자에게 공개할 정보를 선택하세요</p>
          </div>
          <div class="setting-item">
            <div class="setting-info">
              <label class="setting-label">친구 목록 공개 범위</label>
              <span class="setting-value">{{ friendDiscovery }}</span>
            </div>
            <button @click="editFriendDiscovery" class="btn btn-secondary">변경</button>
          </div>
        </div>

        <!-- 차단 관리 -->
        <div class="settings-card">
          <div class="card-header">
            <h2>🚫 차단한 사용자</h2>
          </div>
          <div class="user-list">
            <div v-if="blockedUsers.length === 0" class="empty-state">
              차단한 사용자가 없습니다.
            </div>
            <div v-else v-for="user in blockedUsers" :key="user.userId" class="user-item">
              <img 
                :src="user.profileThumbnailMediaId ? `/api/Media/${user.profileThumbnailMediaId}` : '/src/assets/images/default_profile_image.jpg'" 
                class="user-avatar"
              />
              <div class="user-info">
                <div class="user-name">{{ user.nickname }}</div>
                <div class="user-handle">@{{ user.handle }}</div>
              </div>
              <button @click="unblockUser(user.userId)" class="btn btn-danger">차단 해제</button>
            </div>
          </div>
        </div>

        <!-- 무시 관리 -->
        <div class="settings-card">
          <div class="card-header">
            <h2>🙈 무시한 사용자</h2>
          </div>
          <div class="user-list">
            <div v-if="ignoredUsers.length === 0" class="empty-state">
              무시한 사용자가 없습니다.
            </div>
            <div v-else v-for="user in ignoredUsers" :key="user.userId" class="user-item">
              <img 
                :src="user.profileThumbnailMediaId ? `/api/Media/${user.profileThumbnailMediaId}` : '/src/assets/images/default_profile_image.jpg'" 
                class="user-avatar"
              />
              <div class="user-info">
                <div class="user-name">{{ user.nickname }}</div>
                <div class="user-handle">@{{ user.handle }}</div>
              </div>
              <button @click="unignoreUser(user.userId)" class="btn btn-secondary">무시 해제</button>
            </div>
          </div>
        </div>

        <!-- 계정 관리 -->
        <div class="settings-card danger-zone">
          <div class="card-header">
            <h2>📛 계정 관리</h2>
            <p class="card-description">계정 관련 중요한 작업들입니다</p>
          </div>
          <div class="setting-item">
            <div class="setting-info">
              <label class="setting-label">로그아웃</label>
              <span class="setting-description">현재 세션에서 로그아웃합니다</span>
            </div>
            <button @click="logout" class="btn btn-secondary">로그아웃</button>
          </div>
          <div class="setting-item">
            <div class="setting-info">
              <label class="setting-label">회원 탈퇴</label>
              <span class="setting-description">모든 데이터가 삭제되며 복구할 수 없습니다</span>
            </div>
            <button @click="withdraw" class="btn btn-danger">회원 탈퇴</button>
          </div>
        </div>

        <!-- 정보 및 지원 -->
        <div class="settings-card">
          <div class="card-header">
            <h2>📚 정보 및 지원</h2>
          </div>
          <RouterLink to="/terms" class="setting-link">
            <div class="setting-item">
              <div class="setting-info">
                <label class="setting-label">이용약관</label>
                <span class="setting-description">히스토리 서비스 이용약관을 확인합니다</span>
              </div>
              <span class="arrow-icon">→</span>
            </div>
          </RouterLink>
          <RouterLink to="/privacy" class="setting-link">
            <div class="setting-item">
              <div class="setting-info">
                <label class="setting-label">개인정보처리방침</label>
                <span class="setting-description">개인정보 수집 및 이용에 대한 방침을 확인합니다</span>
              </div>
              <span class="arrow-icon">→</span>
            </div>
          </RouterLink>
        </div>
      </div>
    </main>
  </div>
</template>

<style scoped>
.settings-layout {
  background-color: #f8f9fa;
  min-height: 100vh;
}

.main-content {
  display: flex;
  justify-content: center;
  width: 100%;
  max-width: 1024px;
  margin: 0 auto;
  padding: 24px;
}

.settings-container {
  flex: 1;
  max-width: 800px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.page-header {
  text-align: left;
  margin-bottom: 8px;
}

.page-header h1 {
  font-size: 2.5rem;
  font-weight: 700;
  color: #212529;
  margin: 0 0 8px 0;
}

.page-subtitle {
  font-size: 1.1rem;
  color: #6c757d;
  margin: 0;
}

.settings-card {
  background: white;
  border-radius: 12px;
  border: 1px solid #e9ecef;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
  overflow: hidden;
  transition: box-shadow 0.2s ease;
}

.settings-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.settings-card.danger-zone {
  border-color: #ffc9c9;
  background: linear-gradient(135deg, #fff 0%, #fff9f9 100%);
}

.card-header {
  padding: 20px 24px 16px 24px;
  border-bottom: 1px solid #f1f3f4;
}

.card-header h2 {
  font-size: 1.25rem;
  font-weight: 600;
  color: #212529;
  margin: 0 0 4px 0;
}

.card-description {
  font-size: 0.9rem;
  color: #6c757d;
  margin: 0;
}

.setting-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px;
  border-bottom: 1px solid #f1f3f4;
  gap: 16px;
}

.setting-item:last-child {
  border-bottom: none;
}

.setting-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.setting-label {
  font-weight: 600;
  color: #212529;
  font-size: 1rem;
}

.setting-value {
  font-size: 0.9rem;
  color: #6c757d;
  padding: 4px 8px;
  border-radius: 4px;
  background-color: #f8f9fa;
  display: inline-block;
  width: fit-content;
}

.setting-value.active {
  background-color: #d4edda;
  color: #155724;
  font-weight: 500;
}

.setting-description {
  font-size: 0.85rem;
  color: #868e96;
}

.btn {
  padding: 8px 16px;
  border-radius: 6px;
  border: none;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  font-size: 0.9rem;
  min-width: 80px;
}

.btn-primary {
  background-color: #ed664d;
  color: white;
}

.btn-primary:hover {
  background-color: #d85a47;
  transform: translateY(-1px);
}

.btn-secondary {
  background-color: #6c757d;
  color: white;
}

.btn-secondary:hover {
  background-color: #5a6268;
  transform: translateY(-1px);
}

.btn-danger {
  background-color: #dc3545;
  color: white;
}

.btn-danger:hover {
  background-color: #c82333;
  transform: translateY(-1px);
}

.user-list {
  padding: 0;
}

.empty-state {
  padding: 40px 24px;
  text-align: center;
  color: #6c757d;
  font-style: italic;
}

.user-item {
  display: flex;
  align-items: center;
  padding: 16px 24px;
  border-bottom: 1px solid #f1f3f4;
  gap: 12px;
  transition: background-color 0.2s ease;
}

.user-item:last-child {
  border-bottom: none;
}

.user-item:hover {
  background-color: #f8f9fa;
}

.user-avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid #e9ecef;
}

.user-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.user-name {
  font-weight: 600;
  color: #212529;
}

.user-handle {
  font-size: 0.85rem;
  color: #6c757d;
}

.toggle-container {
  display: flex;
  align-items: center;
  gap: 12px;
}

.toggle-switch {
  position: relative;
  display: inline-block;
  width: 52px;
  height: 28px;
  cursor: pointer;
}

.toggle-switch input {
  opacity: 0;
  width: 0;
  height: 0;
}

.toggle-slider {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #ccc;
  transition: all 0.3s ease;
  border-radius: 28px;
  box-shadow: inset 0 2px 4px rgba(0, 0, 0, 0.1);
}

.toggle-slider:before {
  position: absolute;
  content: "";
  height: 22px;
  width: 22px;
  left: 3px;
  bottom: 3px;
  background-color: white;
  transition: all 0.3s ease;
  border-radius: 50%;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

.toggle-switch input:checked + .toggle-slider {
  background-color: #ed664d;
  box-shadow: inset 0 2px 4px rgba(237, 102, 77, 0.3);
}

.toggle-switch input:checked + .toggle-slider:before {
  transform: translateX(24px);
}

.toggle-switch:hover .toggle-slider {
  box-shadow: inset 0 2px 4px rgba(0, 0, 0, 0.2), 0 0 0 3px rgba(237, 102, 77, 0.1);
}

.toggle-label {
  font-size: 0.9rem;
  font-weight: 500;
  color: #6c757d;
  transition: color 0.2s ease;
  min-width: 40px;
}

.toggle-label.active {
  color: #ed664d;
  font-weight: 600;
}

.notification-select {
  padding: 8px 12px;
  border: 1px solid #e1e5e9;
  border-radius: 6px;
  background-color: white;
  font-size: 0.9rem;
  color: #495057;
  cursor: pointer;
  transition: all 0.2s ease;
  min-width: 140px;
}

.notification-select:hover {
  border-color: #ed664d;
}

.notification-select:focus {
  outline: none;
  border-color: #ed664d;
  box-shadow: 0 0 0 3px rgba(237, 102, 77, 0.1);
}

@media (max-width: 768px) {
  .main-content {
    padding: 16px;
  }
  
  .settings-container {
    gap: 16px;
  }
  
  .page-header h1 {
    font-size: 2rem;
  }
  
  .page-subtitle {
    font-size: 1rem;
  }
  
  .card-header,
  .setting-item {
    padding: 16px 20px;
  }
  
  .setting-item {
    flex-direction: column;
    align-items: flex-start;
    gap: 12px;
  }
  
  .toggle-container {
    align-self: flex-end;
    gap: 8px;
  }
  
  .toggle-switch {
    width: 48px;
    height: 26px;
  }
  
  .toggle-slider:before {
    height: 20px;
    width: 20px;
    left: 3px;
    bottom: 3px;
  }
  
  .toggle-switch input:checked + .toggle-slider:before {
    transform: translateX(22px);
  }
  
  .btn {
    width: 100%;
    min-width: unset;
  }
  
  .user-item {
    padding: 12px 20px;
  }
  
  .user-avatar {
    width: 40px;
    height: 40px;
  }
}

@media (max-width: 480px) {
  .main-content {
    padding: 12px;
  }
  
  .page-header h1 {
    font-size: 1.75rem;
  }
  
  .card-header,
  .setting-item,
  .user-item {
    padding: 12px 16px;
  }
  
  .toggle-container {
    gap: 6px;
  }
  
  .toggle-switch {
    width: 44px;
    height: 24px;
  }
  
  .toggle-slider:before {
    height: 18px;
    width: 18px;
    left: 3px;
    bottom: 3px;
  }
  
  .toggle-switch input:checked + .toggle-slider:before {
    transform: translateX(20px);
  }
  
  .toggle-label {
    font-size: 0.85rem;
  }
}

/* 정보 및 지원 링크 스타일 */
.setting-link {
  text-decoration: none;
  color: inherit;
  display: block;
}

.setting-link:hover .setting-item {
  background-color: #f8f9fa;
}

.arrow-icon {
  color: #6c757d;
  font-size: 1.2rem;
  font-weight: 300;
  transition: transform 0.2s ease;
}

.setting-link:hover .arrow-icon {
  transform: translateX(4px);
  color: #ed664d;
}
</style>
