import React, { useState, useCallback, useEffect, useRef } from 'react';

interface ModalProps {
    triggerId?: string;
    [key: string]: string | undefined;
}

export function ModalComponent({ triggerId }: ModalProps): JSX.Element {
    const [isOpen, setIsOpen] = useState(false);
    const modalRef = useRef<HTMLDivElement>(null);
    const previousFocusRef = useRef<HTMLElement | null>(null);

    const openModal = useCallback(() => {
        previousFocusRef.current = document.activeElement as HTMLElement;
        setIsOpen(true);
    }, []);

    const closeModal = useCallback(() => {
        setIsOpen(false);
        previousFocusRef.current?.focus();
    }, []);

    useEffect(() => {
        if (triggerId) {
            const trigger = document.getElementById(triggerId);
            if (trigger) {
                trigger.addEventListener('click', openModal);
                return () => trigger.removeEventListener('click', openModal);
            }
        }
    }, [triggerId, openModal]);

    useEffect(() => {
        if (!isOpen) return;

        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === 'Escape') closeModal();

            if (e.key === 'Tab' && modalRef.current) {
                const focusable = modalRef.current.querySelectorAll<HTMLElement>(
                    'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
                );
                const first = focusable[0];
                const last = focusable[focusable.length - 1];

                if (e.shiftKey && document.activeElement === first) {
                    e.preventDefault();
                    last.focus();
                } else if (!e.shiftKey && document.activeElement === last) {
                    e.preventDefault();
                    first.focus();
                }
            }
        };

        document.addEventListener('keydown', handleKeyDown);
        document.body.style.overflow = 'hidden';

        return () => {
            document.removeEventListener('keydown', handleKeyDown);
            document.body.style.overflow = '';
        };
    }, [isOpen, closeModal]);

    if (!isOpen) {
        return (
            <button className="react-modal__trigger" onClick={openModal}>
                Open Modal
            </button>
        );
    }

    return (
        <div className="react-modal__overlay" onClick={closeModal} role="presentation">
            <div
                ref={modalRef}
                className="react-modal__dialog"
                role="dialog"
                aria-modal="true"
                aria-label="Modal dialog"
                onClick={(e) => e.stopPropagation()}
            >
                <div className="react-modal__header">
                    <h2 className="react-modal__title">Modal Dialog</h2>
                    <button
                        className="react-modal__close"
                        onClick={closeModal}
                        aria-label="Close modal"
                    >
                        &times;
                    </button>
                </div>
                <div className="react-modal__body">
                    <p>This is a React-powered modal with focus trapping, keyboard navigation, and overlay click handling.</p>
                </div>
                <div className="react-modal__footer">
                    <button className="react-modal__btn react-modal__btn--secondary" onClick={closeModal}>Cancel</button>
                    <button className="react-modal__btn react-modal__btn--primary" onClick={closeModal}>Confirm</button>
                </div>
            </div>
        </div>
    );
}
