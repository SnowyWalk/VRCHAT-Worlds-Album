"use client";


import {Dialog, DialogContent, DialogOverlay} from "@/components/ui/dialog";
import {
    Carousel,
    CarouselApi,
    CarouselContent,
    CarouselItem,
    CarouselNext,
    CarouselPrevious,
} from "@/components/ui/carousel";
import {Card, CardContent} from "@/components/ui/card";
import {replaceExtension} from "@/utils/common-util";
import Image from "next/image";
import Dict = NodeJS.Dict;
import {useEffect, useState} from "react";
import {AspectRatio} from "@/components/ui/aspect-ratio";
import LODImage from "@/components/LODImage";
import {CarouselDemo} from "@/components/CarouselDemo";
import clsx from "clsx";
import {Button} from "@/components/ui/button";


export type ImageViewProps = {
    imageList: Dict<string>[] | null;
    onESCAction: () => void;
};


export default function ImageView({imageList, onESCAction}: ImageViewProps) {
    const [index, setIndex] = useState(0);
    const [api, setApi] = useState<CarouselApi | null>(null)
    const [useHeight, setUseHeight] = useState(true)

    useEffect(() => {
        function updateSize() {
            const vh = window.innerHeight * 0.8
            const vw = window.innerWidth * 0.8
            setUseHeight(vh < vw) // 높이가 더 작으면 h 적용
        }

        updateSize() // 초기 실행
        window.addEventListener("resize", updateSize)
        return () => window.removeEventListener("resize", updateSize)
    }, [])

    useEffect(() => {
        if (!api)
            return;
        const eventHandle = (e: KeyboardEvent) => {
            if (e.key == "Escape") {
                e.preventDefault();
                onESCAction();
            } else if (e.key == "ArrowLeft") {
                e.preventDefault();
                api?.scrollPrev();
            } else if (e.key == "ArrowRight") {
                e.preventDefault();
                api?.scrollNext();
            }
        };

        window.addEventListener("keydown", eventHandle, {passive: false})

        return () => {
            window.removeEventListener("keydown", eventHandle);
        }
    }, [api, onESCAction]);

    useEffect(() => {
        if (!api) return
        const sync = () => setIndex(api.selectedScrollSnap())
        api.on("select", sync)
        api.on("reInit", sync)
        sync() // 초기값 동기화
        return () => {
            api.off("select", sync)
            api.off("reInit", sync)
        }
    }, [api])


    return (
        <div className={"relative w-[100dvw] h-[100dvh]"}>
            <Dialog open>
                {imageList && <DialogOverlay onClick={() => onESCAction()}/>}
            </Dialog>

            <Button type="button" variant="secondary" className="z-70 fixed top-[calc(env(safe-area-inset-top,0px)+20px)] right-[calc(env(safe-area-inset-right,0px)+20px)]
                grid place-items-center h-15 w-15 rounded-full cursor-pointer pointer-events-auto
                transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring
                focus-visible:ring-offset-2 focus-visible:ring-offset-background
                bg-muted text-muted-foreground ring-1 ring-border hover:bg-muted/80 active:bg-muted/70 text-3xl"
                    onClick={() => onESCAction()}>×</Button>

            <div
                className="z-60 fixed left-0 top-0 w-[100dvw] h-[100dvh] flex flex-col items-center justify-around pointer-events-none">
                {/* 캐러셀 영역 */}
                <Carousel opts={{loop: false, align: "center", containScroll: "trimSnaps"}} setApi={setApi}
                          className="w-[80dvw] h-[80dvh] flex items-center justify-center pointer-events-auto">
                    <CarouselContent viewportClassName="w-full"
                                     className="">
                        {
                            imageList && imageList.map((e) => makeWideCarouselItem(e, useHeight))
                        }
                    </CarouselContent>
                </Carousel>

                {/* 하단 네비게이션 */}
                <Card className="w-fit max-w-[90vw] h-[15dvh] pointer-events-auto py-3">
                    <CardContent className="h-full w-full overflow-x-auto overflow-y-hidden touch-pan-x">
                        <div
                            className="grid grid-flow-col auto-cols-[calc(15dvh-48px)] gap-2 h-full w-fit items-center">
                            {
                                imageList && imageList.map((e: Dict<string>, i: number) => makeThumbButton(e, i, () => api?.scrollTo(i), index))
                            }
                        </div>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}

function makeThumbButton(dic: Dict<string>, idx: number, onClickAction: (index: number) => void, selectedIdx: number) {
    return (
        <AspectRatio ratio={1 / 1} key={`${dic['worldId']}-${dic['filename']}`}
                     className={`rounded-lg overflow-hidden border-accent border-[1px] hover:cursor-pointer shrink-0 ring-primary ${selectedIdx === idx ? 'ring-2' : 'hover:ring-1 hover:border-secondary'} select-none`}>
            <Image
                src={`/static/Thumb/${dic['worldId']}/${replaceExtension(dic['filename']!, ".webp")}`}
                alt=""
                fill
                className="object-cover rounded-sm bg-muted"
                loading="lazy"
                decoding="async"
                onClick={() => onClickAction(idx)}
            />
        </AspectRatio>
    )
}


function makeWideCarouselItem(dic: Dict<string>, useHeight: boolean) {
    const w = Number(dic['width'])
    const h = Number(dic['height'])

    return (
        <CarouselItem
            key={`${dic['worldId']}-${dic['filename']}`}
            className="items-center justify-center flex px-5 py-1"
        >
            <div className={clsx("relative overflow-hidden rounded-xl outline-2 aspect-[var(--imageRatio)]",
                useHeight ? "h-[80dvh]" : "w-[80dvw]")}
                 style={{['--imageRatio' as never]: `${w}/${h}`}}>
                <LODImage
                    lowSrc={`/static/Thumb/${dic['worldId']}/${replaceExtension(dic['filename']!, ".webp")}`}
                    highSrc={`/static/View/${dic['worldId']}/${replaceExtension(dic['filename']!, ".webp")}`}
                    alt={dic['filename']!}
                    width={w}
                    height={h}
                    className="object-contain h-full"
                />
            </div>
        </CarouselItem>
    )
}