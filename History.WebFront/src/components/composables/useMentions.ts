import { ref, type Ref } from 'vue';
import type { UserResponseDto } from '@/types';
import { useFriendData } from './useFriendData';

export function useMentions(newPostText: Ref<string>) {
  const { friendsList, loadFriends} = useFriendData();

  const isMentioning = ref(false);
  const mentionSearchText = ref('');
  const mentionSearchResults = ref<UserResponseDto[]>([]);
  const mentionStartIndex = ref(-1);
  const mentionDropdownPosition = ref({ top: 0, left: 0 });
  const selectedMentionIndex = ref(-1);
  let mentionSearchTimeout: number | null = null;

  const handleTextInput = (event: Event) => {
    const target = event.target as HTMLTextAreaElement;
    const cursorPosition = target.selectionStart;
    const text = target.value;

    const lastAtSymbol = text.lastIndexOf('@', cursorPosition - 1);

    if (lastAtSymbol !== -1 && lastAtSymbol < cursorPosition) {
      const searchText = text.substring(lastAtSymbol + 1, cursorPosition);

      if (searchText.includes(' ') || searchText.includes('\n')) {
        isMentioning.value = false;
        mentionSearchResults.value = [];
        return;
      }

      isMentioning.value = true;
      mentionStartIndex.value = lastAtSymbol;
      mentionSearchText.value = searchText;

      const textareaRect = target.getBoundingClientRect();
      mentionDropdownPosition.value = {
        top: textareaRect.bottom + 5,
        left: textareaRect.left
      };

      searchMentions();
    } else {
      isMentioning.value = false;
      mentionSearchResults.value = [];
    }
  };

  const searchMentions = () => {
    if (mentionSearchTimeout) {
      clearTimeout(mentionSearchTimeout);
    }

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
      results = friendsList.value.slice(0, 5);
    } else {
      const filtered = friendsList.value.filter(friend =>
        friend.nickname.toLowerCase().includes(mentionSearchText.value.toLowerCase()) ||
        friend.handle.toLowerCase().includes(mentionSearchText.value.toLowerCase())
      );
      results = filtered.slice(0, 5);
    }

    // useFriendData에서 이미 profileImageUrl을 추가했으므로 여기서는 추가 작업 불필요
    mentionSearchResults.value = results;
    selectedMentionIndex.value = -1;
  };

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

  return {
    isMentioning,
    mentionSearchResults,
    mentionDropdownPosition,
    selectedMentionIndex,
    handleTextInput,
    handleKeyDown,
    selectMention,
    friendsList, 
    loadFriends, 
  };
}