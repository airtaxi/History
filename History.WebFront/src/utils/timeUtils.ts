
/**
 * 주어진 날짜 문자열을 현재 시간과의 상대적인 시간으로 포맷합니다.
 * 예: "방금 전", "5분 전", "3시간 전", "2023-07-07 10:30"
 * @param {string} dateString - 포맷할 날짜 문자열 (ISO 8601 형식).
 * @returns {string} 포맷된 시간 문자열.
 */
export function formatRelativeTime(dateString: string): string {
  const created = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - created.getTime();
  const diffMinutes = Math.floor(diffMs / (1000 * 60));
  const diffHours = Math.floor(diffMinutes / 60);

  if (diffMinutes < 1) return '방금 전';
  if (diffMinutes < 60) return `${diffMinutes}분 전`;
  if (diffHours < 12) return `${diffHours}시간 전`;

  // 12시간 이상이면 날짜와 시간만 출력
  return `${created.getFullYear()}-${(created.getMonth() + 1).toString().padStart(2, '0')}-${created.getDate().toString().padStart(2, '0')} ${created.getHours().toString().padStart(2, '0')}:${created.getMinutes().toString().padStart(2, '0')}`;
}
