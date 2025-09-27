"use client"

import { Carousel, CarouselContent, CarouselItem, CarouselNext, CarouselPrevious } from "@/components/ui/carousel"
import Image from "next/image"

export function PeekCarousel({ items }: { items: { src: string; alt: string }[] }) {
  return (
    <div className="w-full [--peek:16px] sm:[--peek:24px] lg:[--peek:32px]">
      <Carousel
        // Embla 옵션: 중앙 정렬 + 끝단 트리밍
        opts={{ align: "center", containScroll: "trimSnaps" }}
        className="w-full"
      >
        {/* 기본 -ml-4를 상쇄해서 좌측 당김 제거 */}
        <CarouselContent className="ml-0">
          {items.map((it, i) => (
            <CarouselItem
              key={i}
              // 기본 pl-4를 상쇄 + 폭을 100% - 2*peek 로 축소 → 양옆이 자연스럽게 보임
              className="pl-0 basis-[calc(100%-var(--peek)*2)]"
            >
              <div className="relative aspect-[4/3] overflow-hidden rounded-xl">
                <Image
                  src={it.src}
                  alt={it.alt}
                  fill
                  sizes="(max-width:640px) 90vw, (max-width:1024px) 70vw, 60vw"
                  className="object-cover"
                  unoptimized
                />
              </div>
            </CarouselItem>
          ))}
        </CarouselContent>

        <CarouselPrevious className="left-2" />
        <CarouselNext className="right-2" />
      </Carousel>
    </div>
  )
}