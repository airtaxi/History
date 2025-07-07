/**
 * useLongPress
 *
 * 이 컴포저블은 웹 애플리케이션에서 롱 프레스(Long Press) 이벤트를 처리하는 기능을 제공합니다.
 * 특정 DOM 요소에 마우스 또는 터치 이벤트를 통해 롱 프레스 동작을 감지하고,
 * 설정된 지연 시간(delay) 이후에 콜백 함수를 실행합니다.
 *
 * @returns {Object} 롱 프레스 관련 함수들을 포함하는 객체
 * @property {Function} onLongPress - 롱 프레스 이벤트를 설정하는 함수.
 *   @param {Function} callback - 롱 프레스 감지 시 실행될 콜백 함수.
 *   @param {number} [delay=500] - 롱 프레스로 간주할 지연 시간 (밀리초). 기본값은 500ms.
 *   @returns {Object} 롱 프레스 타이머를 시작하고 종료하는 함수를 포함하는 객체.
 *     @property {Function} start - 롱 프레스 타이머를 시작합니다. (예: @mousedown, @touchstart 이벤트에 바인딩)
 *     @property {Function} end - 롱 프레스 타이머를 중지합니다. (예: @mouseup, @touchend, @mouseleave 이벤트에 바인딩)
 */
import { ref, type Ref } from 'vue';

export function useLongPress() {
  let timer: ReturnType<typeof setTimeout> | null = null;

  const onLongPress = (callback: () => void, delay: number = 500) => {
    const start = () => {
      // 기존 타이머가 있다면 클리어하여 중복 실행을 방지
      if (timer) {
        clearTimeout(timer);
      }
      timer = setTimeout(() => {
        callback();
        timer = null; // 타이머 실행 후 초기화
      }, delay);
    };

    const end = () => {
      if (timer) {
        clearTimeout(timer);
        timer = null; // 타이머 종료 후 초기화
      }
    };

    return { start, end };
  };

  return {
    onLongPress,
  };
}
