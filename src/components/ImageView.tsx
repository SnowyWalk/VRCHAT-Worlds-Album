"use client";


import {Dialog, DialogContent, DialogOverlay} from "@/components/ui/dialog";
import {Carousel, CarouselContent, CarouselItem, CarouselNext, CarouselPrevious,} from "@/components/ui/carousel";
import {Card, CardContent} from "@/components/ui/card";
import {replaceExtension} from "@/utils/common-util";
import Image from "next/image";
import Dict = NodeJS.Dict;
import {useState} from "react";
import {AspectRatio} from "@/components/ui/aspect-ratio";


export type ImageViewProps = {
    imageList: Dict<string>[];
};


export default function ImageView({imageList}: ImageViewProps) {
    const [idx, setIdx] = useState(0);

    return (
        <Dialog open>
            <DialogOverlay className={"content-center justify-items-center"}>
                <Carousel opts={{loop: true}} className={"w-[90vw] h-[100vh] flex items-center justify-center"}>
                    <div className={"w-full h-full flex flex-col items-center justify-evenly"}>
                        <CarouselContent className={"px-[100px] h-full"}
                                         viewportClassName={"overflow-hidden py-4 h-[80vh+4] "}>
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
    <CarouselItem key={`${dic['worldId']}-${dic['filename']}`} className="w-full overflow-x-visible max-w-[90vw]">
      <div
        className="relative h-[80dvh] w-[calc(80dvh*var(--ar))] max-w-none mx-auto overflow-hidden rounded-xl outline-2"
        style={{ ['--ar' as never]: w / h }}
      >
        <Image
          src={`/static/Thumb/${dic['worldId']}/${replaceExtension(dic['filename']!, ".webp")}`}
          alt={dic['filename']!}
          fill
          className="object-contain"
          sizes="(max-width:768px) 100vw, 100vw"
          loading="lazy"
          decoding="async"
          unoptimized
        />
      </div>
    </CarouselItem>
  )
}