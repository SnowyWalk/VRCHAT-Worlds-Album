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


export type ImageViewProps = {
    imageList: Dict<string>[];
    onESCAction: () => void;
};


export default function ImageView({imageList, onESCAction}: ImageViewProps) {
    const [idx, setIdx] = useState(0);
    const [api, setApi] = useState<CarouselApi | null>(null)

    useEffect(() => {

        const eventHandle = (e: KeyboardEvent) => {
          if(e.key == "Escape") {
              e.preventDefault();
              onESCAction();
          }
          else if(e.key == "ArrowLeft")
          {
              e.preventDefault();
              api?.scrollPrev();
          }
          else if(e.key == "ArrowRight")
          {
              e.preventDefault();
              api?.scrollNext();
          }
        };

        window.addEventListener("keydown", eventHandle, {passive: false})

        return () => {
            window.removeEventListener("keydown", eventHandle);
        }
    }, [api, onESCAction]);


    return (
        <Dialog open>
            <DialogOverlay className={"content-center justify-items-center"}>
                <Carousel opts={{loop: true}} setApi={setApi} className={"w-[90vw] h-[100vh] flex items-center justify-center"}>
                    <div className={"w-full h-full flex flex-col items-center justify-evenly"}>
                        <CarouselContent className={"h-full"}
                                         viewportClassName={"overflow-hidden py-4"}>
                            {
                                imageList.map(makeWideCarouselItem)
                            }
                        </CarouselContent>
                        <Card>
                            <CardContent>asd</CardContent>
                        </Card>
                    </div>
                    <CarouselPrevious/>
                    <CarouselNext/>
                </Carousel>

            </DialogOverlay>
        </Dialog>
    );
}

function makeWideCarouselItem(dic: Dict<string>) {
  const w = Number(dic['width'])
  const h = Number(dic['height'])

  return (
    <CarouselItem key={`${dic['worldId']}-${dic['filename']}`} >
      <div
        className="relative h-[80dvh] w-[calc(80dvh*var(--ar))] max-w-[90vw] mx-auto overflow-hidden rounded-xl outline-2"
        style={{ ['--ar' as never]: w / h }}
      >
        <LODImage
          lowSrc={`/static/Thumb/${dic['worldId']}/${replaceExtension(dic['filename']!, ".webp")}`}
          highSrc={`/static/View/${dic['worldId']}/${replaceExtension(dic['filename']!, ".webp")}`}
          alt={dic['filename']!}
          className="object-contain"
          sizes="(max-width:768px) 100vw, 90vw"
        />
      </div>
    </CarouselItem>
  )
}