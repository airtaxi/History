// Timeline interop: theme, infinite scroll, scroll-to-top detection, carousel
// indicators, and pull-to-refresh. All DOM-only concerns stay here so high-frequency
// values never round-trip through the Blazor render loop.

window.timelineInterop = (() => {
    let dotnetRef = null;
    let sentinelObserver = null;
    let scrollTicking = false;
    let feedObserver = null;
    const carouselObserved = new Set();
    let masonry = null;
    let masonryColumns = 0;
    let masonryUpdateScheduled = false;
    let masonryResizeScheduled = false;

    function setTheme(name) {
        document.documentElement.setAttribute('data-theme', name);
    }

    function scrollToTop() {
        window.scrollTo({ top: 0, behavior: 'auto' });
    }

    function onScroll() {
        if (scrollTicking) return;
        scrollTicking = true;
        window.requestAnimationFrame(() => {
            scrollTicking = false;
            if (dotnetRef) dotnetRef.invokeMethodAsync('ScrollChanged', window.scrollY || document.documentElement.scrollTop);
        });
    }

    // Mirrors BaseWrappedMediaContentsViewModel.CalculateNewHeight: the carousel height
    // follows the CURRENT position image's aspect ratio, capped at a 1:1 ratio of the
    // container width; every item shares that height (AspectFill crops the rest).
    function updateCarouselHeight(track) {
        const width = track.clientWidth;
        if (!width) return;
        const first = track.firstElementChild;
        if (!first) return;
        const count = track.children.length;
        const index = Math.min(Math.round(track.scrollLeft / first.getBoundingClientRect().width), count - 1);
        const item = track.children[index];
        const ratio = parseFloat(item.dataset.ratio) || 1;
        const height = Math.min(width / ratio, width);
        let changed = false;
        for (const child of track.children) {
            if (child.style.height === `${height}px`) continue;
            child.style.height = `${height}px`;
            changed = true;
        }
        if (changed) scheduleMasonryUpdate();
    }

    // Mirrors TimelinePage.OnSizeChanged's StaggeredItemsLayout: span = Width / 700 + 1,
    // single column stays in normal flow (LinearItemsLayout), 2+ columns use Masonry.
    let masonryColumnWidth = 0;

    function getMasonryColumns() { return Math.floor(window.innerWidth / 700) + 1; }

    function clearMasonryItemStyles(grid) {
        for (const card of grid.querySelectorAll('.card')) {
            card.style.position = '';
            card.style.left = '';
            card.style.top = '';
            card.style.width = '';
            card.style.transform = '';
        }
    }

    function applyMasonryItemWidths(grid) {
        for (const card of grid.querySelectorAll('.card')) card.style.width = `${masonryColumnWidth}px`;
    }

    function destroyMasonry() {
        if (!masonry) return;
        masonry.destroy();
        masonry = null;
        masonryColumns = 0;
        masonryColumnWidth = 0;
        const grid = document.getElementById('masonry-grid');
        if (grid) {
            grid.classList.remove('masonry-active');
            grid.style.height = '';
            clearMasonryItemStyles(grid);
        }
    }

    function initMasonry() {
        const grid = document.getElementById('masonry-grid');
        if (!grid) return;

        const columns = getMasonryColumns();
        if (columns <= 1) { destroyMasonry(); return; }

        const gutter = 12; // matches the 6px card margins of the single-column flow
        const padding = 12;
        const columnWidth = (grid.clientWidth - padding - gutter * (columns - 1)) / columns;

        // The column count can stay the same while the width changes (fold/rotate);
        // Masonry does not recompute columnWidth on its own, so update it explicitly.
        if (masonry && masonryColumns === columns) {
            if (columnWidth !== masonryColumnWidth) {
                masonryColumnWidth = columnWidth;
                masonry.options.columnWidth = columnWidth;
                applyMasonryItemWidths(grid);
            }
            masonry.layout();
            return;
        }

        destroyMasonry();

        masonryColumnWidth = columnWidth;
        masonry = new Masonry(grid, {
            itemSelector: '.card',
            columnWidth: masonryColumnWidth,
            gutter: gutter,
            transitionDuration: 0,
            resize: false
        });
        masonryColumns = columns;
        grid.classList.add('masonry-active');
        applyMasonryItemWidths(grid);
    }

    function scheduleMasonryUpdate() {
        if (!masonry || masonryUpdateScheduled) return;
        masonryUpdateScheduled = true;
        requestAnimationFrame(() => {
            masonryUpdateScheduled = false;
            if (!masonry) return;
            const grid = document.getElementById('masonry-grid');
            if (grid) applyMasonryItemWidths(grid);
            masonry.reloadItems();
            masonry.layout();
        });
    }

    function onMasonryResize() {
        if (masonryResizeScheduled) return;
        masonryResizeScheduled = true;
        requestAnimationFrame(() => {
            masonryResizeScheduled = false;
            initMasonry();
        });
    }

    function attachCarousels() {
        const feed = document.getElementById('feed');
        if (!feed) return;
        const tracks = feed.querySelectorAll('[data-interop-carousel]');
        for (const track of tracks) {
            if (carouselObserved.has(track)) continue;
            carouselObserved.add(track);
            const container = track.parentElement;
            if (container) new ResizeObserver(() => updateCarouselHeight(track)).observe(container);
        }
    }

    // The muted ATTRIBUTE has no effect on videos inserted after page parse (Chromium
    // applies it at parse time only), so the property must be set directly. Timeline
    // inline videos are always muted, mirroring VideoViewModel.ShouldMute = true.
    function muteFeedVideos() {
        const feed = document.getElementById('feed');
        if (!feed) return;
        for (const video of feed.querySelectorAll('video')) video.muted = true;
    }

    function watchFeedForCarousels() {
        const feed = document.getElementById('feed');
        if (!feed || feedObserver) return;
        feedObserver = new MutationObserver(() => { attachCarousels(); muteFeedVideos(); scheduleMasonryUpdate(); });
        feedObserver.observe(feed, { childList: true, subtree: true });
    }

    // Capture-phase handler: keeps carousel position indicators and heights in sync
    // without involving Blazor on every swipe frame. 'load' doesn't bubble, so the
    // capture phase is used to catch image loads as well.
    function onCaptureScroll(event) {
        const target = event.target;
        if (!target || !target.closest || target === document) return;
        const track = target.closest ? target.closest('[data-interop-carousel]') : null;
        if (!track) return;
        updateCarouselHeight(track);
        const carousel = track.parentElement;
        if (!carousel) return;
        const indicator = carousel.querySelector('.carousel-indicator');
        if (!indicator) return;
        const item = track.firstElementChild;
        const index = item ? Math.round(track.scrollLeft / item.getBoundingClientRect().width) : 0;
        indicator.textContent = `${index + 1} / ${track.children.length}`;
    }

    function onImageLoaded(event) {
        if (!(event.target instanceof HTMLImageElement)) return;
        const item = event.target.closest('.carousel-item');
        if (!item) return;
        const track = event.target.closest('[data-interop-carousel]');
        if (!track) return;
        if (event.target.naturalWidth > 0) item.dataset.ratio = (event.target.naturalWidth / event.target.naturalHeight).toFixed(4);
        updateCarouselHeight(track);
        scheduleMasonryUpdate();
    }

    // Chromium-based webviews never fire the 'longpress' DOM event, so long-press is
    // detected from raw touch input here. A 500ms hold with minimal movement fires the
    // component's copy method once; the native webview long-press (haptic/context menu)
    // is suppressed platform-side and via the contextmenu preventDefault above.
    const longPressCleanups = new WeakMap();

    function attachLongPress(element, dotNetRef, methodName) {
        if (!element || longPressCleanups.has(element)) return;
        let timer = null;
        let startX = 0;
        let startY = 0;

        const cancel = () => { if (timer) { clearTimeout(timer); timer = null; } };
        const start = (x, y) => {
            startX = x;
            startY = y;
            cancel();
            timer = setTimeout(() => {
                timer = null;
                dotNetRef.invokeMethodAsync(methodName);
            }, 500);
        };
        const onTouchStart = (event) => { const touch = event.touches[0]; start(touch.clientX, touch.clientY); };
        const onTouchMove = (event) => {
            const touch = event.touches[0];
            if (Math.abs(touch.clientX - startX) > 10 || Math.abs(touch.clientY - startY) > 10) cancel();
        };
        const onTouchEnd = () => cancel();
        const onTouchCancel = () => cancel();

        element.addEventListener('touchstart', onTouchStart, { passive: true });
        element.addEventListener('touchmove', onTouchMove, { passive: true });
        element.addEventListener('touchend', onTouchEnd, { passive: true });
        element.addEventListener('touchcancel', onTouchCancel, { passive: true });

        // Mouse-only fallback for desktop debugging. Skipped on touch devices so the
        // compatibility mouse events don't double-fire the copy.
        let onMouseDown = null, onMouseUp = null, onMouseLeave = null;
        if (!('ontouchstart' in window)) {
            onMouseDown = () => { cancel(); timer = setTimeout(() => { timer = null; dotNetRef.invokeMethodAsync(methodName); }, 500); };
            onMouseUp = () => cancel();
            onMouseLeave = () => cancel();
            element.addEventListener('mousedown', onMouseDown);
            element.addEventListener('mouseup', onMouseUp);
            element.addEventListener('mouseleave', onMouseLeave);
        }

        longPressCleanups.set(element, () => {
            element.removeEventListener('touchstart', onTouchStart);
            element.removeEventListener('touchmove', onTouchMove);
            element.removeEventListener('touchend', onTouchEnd);
            element.removeEventListener('touchcancel', onTouchCancel);
            if (onMouseDown) {
                element.removeEventListener('mousedown', onMouseDown);
                element.removeEventListener('mouseup', onMouseUp);
                element.removeEventListener('mouseleave', onMouseLeave);
            }
        });
    }

    function detachLongPress(element) {
        const cleanup = longPressCleanups.get(element);
        if (cleanup) { cleanup(); longPressCleanups.delete(element); }
    }

    // Mirrors the XAML MediaContentTemplate's Unloaded event: when a carousel leaves
    // the viewport, its inline videos must reset to thumbnail + play overlay. MAUI
    // CollectionView recycles items out of view, firing Unloaded; BlazorWebView keeps
    // everything in the DOM, so an IntersectionObserver is the equivalent.
    const videoVisibilityObservers = new WeakMap();

    function attachVideoVisibility(element, dotNetRef, methodName) {
        if (!element || videoVisibilityObservers.has(element)) return;
        const observer = new IntersectionObserver((entries) => {
            for (const entry of entries) {
                if (!entry.isIntersecting) dotNetRef.invokeMethodAsync(methodName);
            }
        }, { root: null, threshold: 0 });
        observer.observe(element);
        videoVisibilityObservers.set(element, observer);
    }

    function detachVideoVisibility(element) {
        const observer = videoVisibilityObservers.get(element);
        if (observer) { observer.disconnect(); videoVisibilityObservers.delete(element); }
    }

    function attachPullToRefresh() {
        const feed = document.getElementById('feed');
        const indicator = document.getElementById('pull-indicator');
        if (!feed || !indicator) return;

        let startY = 0;
        let distance = 0;
        let pulling = false;

        const threshold = 55;

        feed.addEventListener('touchstart', (event) => {
            if (window.scrollY > 0) return;
            pulling = true;
            startY = event.touches[0].clientY;
            distance = 0;
        }, { passive: true });

        feed.addEventListener('touchmove', (event) => {
            if (!pulling) return;
            const currentY = event.touches[0].clientY;
            distance = currentY - startY;
            if (distance > 0 && window.scrollY <= 0) {
                event.preventDefault();
                const pull = Math.min(distance * 0.5, 70);
                indicator.style.height = `${pull}px`;
                indicator.textContent = pull >= threshold ? '놓아서 새로고침' : '당겨서 새로고침';
            }
        }, { passive: false });

        feed.addEventListener('touchend', () => {
            if (!pulling) return;
            pulling = false;
            if (distance * 0.5 >= threshold && dotnetRef) dotnetRef.invokeMethodAsync('PullToRefreshAsync');
            indicator.style.height = '0px';
            indicator.textContent = '';
            distance = 0;
        }, { passive: true });
    }

    function init(ref, theme) {
        dotnetRef = ref;
        setTheme(theme);

        // Long-press copy is handled by the app (TextContents); suppress the native
        // context menu / text selection that would otherwise appear.
        document.addEventListener('contextmenu', (event) => event.preventDefault());
        document.addEventListener('selectionchange', () => window.getSelection()?.removeAllRanges());

        const sentinel = document.getElementById('load-more-sentinel');
        if (sentinel && !sentinelObserver) {
            sentinelObserver = new IntersectionObserver((entries) => {
                if (entries.length > 0 && entries[0].isIntersecting && dotnetRef) dotnetRef.invokeMethodAsync('LoadMoreAsync');
            }, { root: null, rootMargin: '800px 0px 800px 0px', threshold: 0 });
            sentinelObserver.observe(sentinel);
        }

        window.addEventListener('scroll', onScroll, { passive: true });
        document.addEventListener('scroll', onCaptureScroll, { capture: true, passive: true });
        document.addEventListener('load', onImageLoaded, { capture: true, passive: true });
        window.addEventListener('resize', onMasonryResize, { passive: true });
        attachPullToRefresh();
        attachCarousels();
        watchFeedForCarousels();
        muteFeedVideos();
        initMasonry();
    }

    return { init, setTheme, scrollToTop, attachLongPress, detachLongPress, attachVideoVisibility, detachVideoVisibility, initMasonry, destroyMasonry };
})();
