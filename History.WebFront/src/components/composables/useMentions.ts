import { ref, watch } from 'vue';
import type { Ref } from 'vue';
import type { UserResponseDto } from '@/types';

// useMentions는 이제 newPostText 뿐만 아니라 friendsList와 loadFriends도 받습니다.
export function useMentions(
  newPostText: Ref<string>,
  friendsList: Ref<UserResponseDto[]>,
  loadFriends: () => Promise<void>
) {
  const isMentioning = ref(false);
  const mentionSearchResults = ref<UserResponseDto[]>([]);
  const mentionDropdownPosition = ref({ top: 0, left: 0 });
  const selectedMentionIndex = ref(-1);
  const mentionSearchText = ref('');
  const mentionStartIndex = ref(-1);

  // 멘션 검색을 수행하는 내부 함수
  const performMentionSearch = () => {
    let results: UserResponseDto[] = [];
    if (!mentionSearchText.value) {
      // 검색어가 없으면 친구 목록 상위 5명을 보여줍니다.
      results = friendsList.value.slice(0, 5);
    } else {
      // 검색어가 있으면 친구 목록에서 필터링합니다.
      const searchTerm = mentionSearchText.value.toLowerCase();
      results = friendsList.value
        .filter(friend =>
          friend.nickname.toLowerCase().includes(searchTerm) ||
          friend.handle.toLowerCase().includes(searchTerm)
        )
        .slice(0, 5);
    }
    mentionSearchResults.value = results;
    selectedMentionIndex.value = -1; // 검색 결과 변경 시 선택 인덱스 초기화
  };

  // @ 멘션을 위한 친구 검색 (디바운싱은 생략, 필요 시 추가)
  const searchMentions = () => {
    if (friendsList.value.length === 0) {
      // 친구 목록이 비어있으면 로드한 후 검색합니다.
      loadFriends().then(() => performMentionSearch());
    } else {
      performMentionSearch();
    }
  };

  const handleTextInput = (event: Event) => {
    const target = event.target as HTMLTextAreaElement;
    const cursorPosition = target.selectionStart;
    const text = target.value;
    const lastAtSymbol = text.lastIndexOf('@', cursorPosition - 1);

    if (lastAtSymbol !== -1 && text.substring(lastAtSymbol + 1, cursorPosition).trim().length === text.substring(lastAtSymbol + 1, cursorPosition).length) {
      isMentioning.value = true;
      mentionStartIndex.value = lastAtSymbol;
      mentionSearchText.value = text.substring(lastAtSymbol + 1, cursorPosition);
      
      const textareaRect = target.getBoundingClientRect();
      mentionDropdownPosition.value = {
        top: textareaRect.bottom + 5,
        left: textareaRect.left,
      };
      
      searchMentions();
    } else {
      isMentioning.value = false;
    }
  };

  const selectMention = (user: UserResponseDto) => {
    const text = newPostText.value;
    const beforeMention = text.substring(0, mentionStartIndex.value);
    const afterCursor = text.substring(mentionStartIndex.value + mentionSearchText.value.length + 1);

    newPostText.value = `${beforeMention}@${user.nickname} ${afterCursor}`;
    isMentioning.value = false;
  };

  const handleKeyDown = (event: KeyboardEvent) => {
    if (!isMentioning.value || mentionSearchResults.value.length === 0) return;

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        selectedMentionIndex.value = (selectedMentionIndex.value + 1) % mentionSearchResults.value.length;
        break;
      case 'ArrowUp':
        event.preventDefault();
        selectedMentionIndex.value = (selectedMentionIndex.value - 1 + mentionSearchResults.value.length) % mentionSearchResults.value.length;
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
        break;
    }
  };

  watch(isMentioning, (newValue) => {
    if (!newValue) {
      mentionSearchResults.value = [];
      selectedMentionIndex.value = -1;
    }
  });

  return {
    isMentioning,
    mentionSearchResults,
    mentionDropdownPosition,
    selectedMentionIndex,
    handleTextInput,
    handleKeyDown,
    selectMention,
  };
}