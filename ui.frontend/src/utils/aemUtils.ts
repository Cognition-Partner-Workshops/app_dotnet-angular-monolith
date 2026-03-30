/**
 * AEM utility functions for React components
 */

export function getAEMImageUrl(path: string, width?: number): string {
    if (!path) return '';
    if (width) {
        return `${path}/_jcr_content/renditions/cq5dam.web.${width}.${width}.jpeg`;
    }
    return path;
}

export function getAEMPageUrl(path: string): string {
    if (!path) return '#';
    if (path.endsWith('.html')) return path;
    return `${path}.html`;
}

export function stripHtmlTags(html: string): string {
    const div = document.createElement('div');
    div.innerHTML = html;
    return div.textContent || div.innerText || '';
}

export function truncateText(text: string, maxLength: number): string {
    if (!text || text.length <= maxLength) return text;
    return text.substring(0, maxLength).trim() + '...';
}

export function isAuthorMode(): boolean {
    return typeof window !== 'undefined' && (
        window.location.pathname.includes('/editor.html') ||
        document.querySelector('body.aem-AuthorLayer-Edit') !== null
    );
}

export function debounce<T extends (...args: unknown[]) => void>(
    func: T,
    wait: number
): (...args: Parameters<T>) => void {
    let timeout: ReturnType<typeof setTimeout>;
    return (...args: Parameters<T>) => {
        clearTimeout(timeout);
        timeout = setTimeout(() => func(...args), wait);
    };
}

export function formatDate(dateString: string, locale: string = 'en-US'): string {
    try {
        const date = new Date(dateString);
        return date.toLocaleDateString(locale, {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
        });
    } catch {
        return dateString;
    }
}
