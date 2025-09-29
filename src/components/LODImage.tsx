"use client";

import Image from "next/image";
import {useState} from "react";
import {cn} from "@/lib/utils";

export default function LODImage({
                                     lowSrc, highSrc, alt, className, width, height
                                 }: {
    lowSrc: string;
    highSrc: string;
    alt: string;
    width: number;
    height: number;
    className?: string
}) {
    const [hiLoaded, setHiLoaded] = useState(false)
    const [hideLow, setHideLow] = useState(false)

        {/*<div className={`relative h-full aspect-[${width}/${height}]`}>*/}
    return (
        <div className={`relative h-full aspect-[var(--imageRatio)]`}>
            {/* 저용량: 먼저 보이게 */}
            <Image
                src={lowSrc}
                alt={alt}
                fill
                className={cn(`object-contain transition-opacity duration-1000  ${hideLow ? "opacity-0" : "opacity-100"} select-none`, className)}
                decoding="async"
                loading="eager"      // 저용량은 즉시
                unoptimized
                aria-hidden={hideLow || undefined}
                draggable={false}
                priority
            />
            {/* 고용량: 로드 완료되면 페이드 인 */}
            <Image
                src={highSrc}
                alt={alt}
                fill
                className={cn(`object-contain transition-opacity duration-300 ${hiLoaded ? "opacity-100" : "opacity-0"} select-none`, className)}
                decoding="async"
                loading="lazy"
                unoptimized
                onLoad={() => setHiLoaded(true)}
                onTransitionEnd={() => setHideLow(true)}
                onError={() => {
                    // 고용량 실패 시 저용량 유지
                    setHiLoaded(false)
                    setHideLow(false)
                }}
                draggable={false}
            />
        </div>
    );
}