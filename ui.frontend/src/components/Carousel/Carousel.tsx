import React, { useState, useCallback, useEffect, useRef } from 'react';

interface CarouselProps {
    autoplay?: string;
    delay?: string;
    [key: string]: string | undefined;
}

export function CarouselComponent({ autoplay, delay = '5000' }: CarouselProps): JSX.Element {
    const [activeIndex, setActiveIndex] = useState(0);
    const [isPlaying, setIsPlaying] = useState(autoplay === 'true');
    const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

    const slides = [
        { title: 'Custom Components', description: 'Build reusable AEM components with HTL and React' },
        { title: 'Content Management', description: 'Manage content fragments and experience fragments' },
        { title: 'Workflow Automation', description: 'Automate content approval and publishing workflows' },
        { title: 'Search & Discovery', description: 'Full-text search with AEM QueryBuilder integration' },
    ];

    const goToSlide = useCallback((index: number) => {
        setActiveIndex((index + slides.length) % slides.length);
    }, [slides.length]);

    const nextSlide = useCallback(() => goToSlide(activeIndex + 1), [activeIndex, goToSlide]);
    const prevSlide = useCallback(() => goToSlide(activeIndex - 1), [activeIndex, goToSlide]);

    useEffect(() => {
        if (isPlaying) {
            timerRef.current = setInterval(nextSlide, parseInt(delay, 10));
        }
        return () => {
            if (timerRef.current) clearInterval(timerRef.current);
        };
    }, [isPlaying, delay, nextSlide]);

    return (
        <div className="react-carousel" role="region" aria-label="Carousel" aria-roledescription="carousel">
            <div className="react-carousel__slides">
                {slides.map((slide, index) => (
                    <div
                        key={index}
                        className={`react-carousel__slide ${index === activeIndex ? 'react-carousel__slide--active' : ''}`}
                        role="tabpanel"
                        aria-hidden={index !== activeIndex}
                    >
                        <h3>{slide.title}</h3>
                        <p>{slide.description}</p>
                    </div>
                ))}
            </div>
            <div className="react-carousel__controls">
                <button className="react-carousel__prev" onClick={prevSlide} aria-label="Previous slide">&lsaquo;</button>
                <button
                    className="react-carousel__playpause"
                    onClick={() => setIsPlaying(!isPlaying)}
                    aria-label={isPlaying ? 'Pause' : 'Play'}
                >
                    {isPlaying ? '⏸' : '▶'}
                </button>
                <button className="react-carousel__next" onClick={nextSlide} aria-label="Next slide">&rsaquo;</button>
            </div>
            <div className="react-carousel__indicators" role="tablist">
                {slides.map((_, index) => (
                    <button
                        key={index}
                        className={`react-carousel__indicator ${index === activeIndex ? 'react-carousel__indicator--active' : ''}`}
                        role="tab"
                        aria-selected={index === activeIndex}
                        aria-label={`Slide ${index + 1}`}
                        onClick={() => goToSlide(index)}
                    />
                ))}
            </div>
        </div>
    );
}
