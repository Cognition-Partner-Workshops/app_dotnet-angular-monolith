/**
 * DevinReactAEM - Base JavaScript
 * Vanilla JS behaviors for components that work alongside React hydration
 */
(function() {
    'use strict';

    /* === Accordion === */
    function initAccordion() {
        document.querySelectorAll('[data-cmp-hook-accordion="button"]').forEach(function(button) {
            button.addEventListener('click', function() {
                var item = this.closest('[data-cmp-hook-accordion="item"]');
                var panel = item.querySelector('[data-cmp-hook-accordion="panel"]');
                var isExpanded = this.getAttribute('aria-expanded') === 'true';
                var accordion = this.closest('.cmp-accordion');
                var singleExpansion = accordion && accordion.getAttribute('data-cmp-single-expansion') === 'true';

                if (singleExpansion) {
                    accordion.querySelectorAll('[data-cmp-hook-accordion="button"]').forEach(function(btn) {
                        btn.setAttribute('aria-expanded', 'false');
                        var p = btn.closest('[data-cmp-hook-accordion="item"]').querySelector('[data-cmp-hook-accordion="panel"]');
                        if (p) p.setAttribute('hidden', 'hidden');
                    });
                }

                this.setAttribute('aria-expanded', String(!isExpanded));
                if (isExpanded) {
                    panel.setAttribute('hidden', 'hidden');
                } else {
                    panel.removeAttribute('hidden');
                }
            });
        });
    }

    /* === Tabs === */
    function initTabs() {
        document.querySelectorAll('.cmp-tabs').forEach(function(tabsContainer) {
            var tabs = tabsContainer.querySelectorAll('[data-cmp-hook-tabs="tab"]');
            var panels = tabsContainer.querySelectorAll('[data-cmp-hook-tabs="tabpanel"]');

            tabs.forEach(function(tab, index) {
                tab.addEventListener('click', function() {
                    tabs.forEach(function(t) { t.setAttribute('aria-selected', 'false'); });
                    panels.forEach(function(p) { p.setAttribute('aria-hidden', 'true'); });
                    this.setAttribute('aria-selected', 'true');
                    if (panels[index]) panels[index].setAttribute('aria-hidden', 'false');
                });
            });
        });
    }

    /* === Carousel === */
    function initCarousel() {
        document.querySelectorAll('.cmp-carousel').forEach(function(carousel) {
            var items = carousel.querySelectorAll('[data-cmp-hook-carousel="item"]');
            var indicators = carousel.querySelectorAll('[data-cmp-hook-carousel="indicator"]');
            var prevBtn = carousel.querySelector('[data-cmp-hook-carousel="previous"]');
            var nextBtn = carousel.querySelector('[data-cmp-hook-carousel="next"]');
            var autoplay = carousel.getAttribute('data-cmp-autoplay') === 'true';
            var delay = parseInt(carousel.getAttribute('data-cmp-delay'), 10) || 5000;
            var current = 0;
            var interval;

            function showSlide(idx) {
                items.forEach(function(item) { item.classList.remove('cmp-carousel__item--active'); });
                indicators.forEach(function(ind) { ind.classList.remove('cmp-carousel__indicator--active'); });
                if (items[idx]) items[idx].classList.add('cmp-carousel__item--active');
                if (indicators[idx]) indicators[idx].classList.add('cmp-carousel__indicator--active');
                var itemsContainer = carousel.querySelector('.cmp-carousel__items');
                if (itemsContainer) itemsContainer.style.transform = 'translateX(-' + (idx * 100) + '%)';
                current = idx;
            }

            if (prevBtn) prevBtn.addEventListener('click', function() { showSlide((current - 1 + items.length) % items.length); });
            if (nextBtn) nextBtn.addEventListener('click', function() { showSlide((current + 1) % items.length); });
            indicators.forEach(function(ind, i) { ind.addEventListener('click', function() { showSlide(i); }); });

            if (autoplay && items.length > 1) {
                interval = setInterval(function() { showSlide((current + 1) % items.length); }, delay);
                carousel.addEventListener('mouseenter', function() { clearInterval(interval); });
                carousel.addEventListener('mouseleave', function() {
                    interval = setInterval(function() { showSlide((current + 1) % items.length); }, delay);
                });
            }
        });
    }

    /* === Modal === */
    function initModal() {
        document.querySelectorAll('.cmp-modal').forEach(function(modal) {
            var trigger = modal.querySelector('[data-cmp-hook-modal="trigger"]');
            var overlay = modal.querySelector('[data-cmp-hook-modal="overlay"]');
            var closeBtns = modal.querySelectorAll('[data-cmp-hook-modal="close"]');

            function openModal() { if (overlay) overlay.removeAttribute('hidden'); document.body.style.overflow = 'hidden'; }
            function closeModal() { if (overlay) overlay.setAttribute('hidden', 'hidden'); document.body.style.overflow = ''; }

            if (trigger) trigger.addEventListener('click', openModal);
            closeBtns.forEach(function(btn) { btn.addEventListener('click', closeModal); });
            if (overlay) overlay.addEventListener('click', function(e) {
                if (e.target === overlay) closeModal();
            });
            document.addEventListener('keydown', function(e) {
                if (e.key === 'Escape' && overlay && !overlay.hasAttribute('hidden')) closeModal();
            });
        });
    }

    /* === Navigation Toggle === */
    function initNavigation() {
        document.querySelectorAll('[data-cmp-hook-navigation="toggle"]').forEach(function(toggle) {
            toggle.addEventListener('click', function() {
                var nav = this.closest('.cmp-navigation');
                var items = nav.querySelector('.cmp-navigation__items');
                if (items) items.classList.toggle('cmp-navigation__items--open');
            });
        });
    }

    /* === Initialize all components === */
    function init() {
        initAccordion();
        initTabs();
        initCarousel();
        initModal();
        initNavigation();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
