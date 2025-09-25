"use client";

import {Card, CardContent, CardFooter, CardHeader} from "@/components/ui/card";
import {AspectRatio} from "@/components/ui/aspect-ratio";
import {Skeleton} from "@/components/ui/skeleton";
import {useState} from "react";
import Dict = NodeJS.Dict;
import {Separator} from "@/components/ui/separator";
import Image from "next/image";

export type WorldCardProps = {
    worldId: string;
    worldName: string;
    authorId: string;
    authorName: string;
    imageUrl: string;
    capacity: number;
    visits: number;
    favorites: number;
    heat: number;
    popularity: number;
    tags: string[];
    imageList: Dict<string>[];
    category: string[] | null;
    description: string | null;
    dataCreatedAt: Date;
    lastFolderModifiedAt: Date;
};

export default function WorldCard({
                                      worldId,
                                      worldName,
                                      authorId,
                                      authorName,
                                      imageUrl,
                                      capacity,
                                      visits,
                                      favorites,
                                      heat,
                                      popularity,
                                      tags,
                                      imageList,
                                      category,
                                      description,
                                      dataCreatedAt,
                                      lastFolderModifiedAt,
                                  }: WorldCardProps) {
    const [loaded, setLoaded] = useState(false);
    const [error, setError] = useState(false);

    console.log("WorldCardProps:", {
        worldName,
        capacity,
        visits,
        favorites,
        heat,
        popularity,
        tags,
        imageList,
        category,
        description,
        dataCreatedAt,
        lastFolderModifiedAt
    });

    return (
        <Card className="overflow-hidden py-0 justify-between items-center gap-0">


            <AspectRatio ratio={4 / 3}>
                <CardHeader className="p-0 relative h-full">
                    {!loaded && !error && (
                        <Skeleton className="absolute inset-0 w-full h-full rounded-none"/>
                    )}
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                        src={imageUrl}
                        alt={worldName}
                        className={`absolute inset-0 w-full h-full object-cover bg-muted transition-opacity duration-300 ${loaded ? "opacity-100" : "opacity-0"}`}
                        loading="lazy"
                        onLoad={() => setLoaded(true)}
                        onError={() => {
                            setError(true);
                            setLoaded(false);
                        }}
                    />
                    {error && (
                        <div
                            className="absolute inset-0 flex items-center justify-center text-sm text-muted-foreground bg-muted">
                            이미지 로드 실패
                        </div>
                    )}
                </CardHeader>
            </AspectRatio>


            <CardContent className="flex flex-col justify-between justify-start items-center w-full grow gap-0 px-4.5">
                <div
                    className="text-lg font-bold text-center my-3 p-0 min-h-[52px] flex flex-col items-center w-full grow-0 justify-center leading-6.5">{worldName}</div>
                <div className="text-sm text-center mb-2 text-muted-foreground grow-0">{authorName}</div>
                <Separator className="m-0"/>
            </CardContent>


            <CardFooter className="flex flex-col justify-between justify-start items-center w-full grow gap-0 px-4.5">
                {tags?.length > 0 && (
                    <div className="m-5 p-0 flex flex-wrap gap-2">
                        {tags.filter((t) => t.startsWith('author_tag_')).map((t) => (
                            <span
                                key={t}
                                className="inline-flex items-center rounded-md border px-2 py-0.5 text-xs font-medium bg-secondary text-secondary-foreground"
                            >
                                {t.replace(/^author_tag_/, '')}
                            </span>
                        ))}
                    </div>
                )}

                {/* 썸네일 리스트: 최대 6개, 초과 시 6번째 위에 반투명 오버레이 +N */}
                {Array.isArray(imageList) && imageList.length > 0 && (
                    <div className="mt-2 w-full">
                        <div className="grid grid-cols-6 gap-1">
                            {imageList.slice(0, 6).map((dic: Dict<string>, i: number) => {
                                return (
                                    // eslint-disable-next-line @next/next/no-img-element
                                    <AspectRatio ratio={1 / 1} key={`${dic['worldId']}-${dic['filename']}`}
                                                 className="rounded-lg overflow-hidden border-accent border-[1px]">
                                        <Image
                                            src={`/static/Thumb/${dic['worldId']}/${replaceExtension(dic['filename']!, ".webp")}`}
                                            alt=""
                                            fill
                                            className="h-full w-full object-cover rounded-sm bg-muted"
                                            loading="lazy"
                                        />

                                        {
                                            i == 5 && Math.max(0, imageList.length - 6) > 0 &&
                                            <div
                                                className="absolute inset-0 bg-black/50 flex items-center justify-center">
                                                <span
                                                    className="text-base font-semibold text-white">+{Math.max(0, imageList.length - 6)}</span>
                                            </div>
                                        }

                                    </AspectRatio>
                                );
                            })}
                        </div>
                    </div>
                )}
                {/*<Separator className="m-0"/>*/}
                {/*{visits !== undefined && <span className="text-xs">방문 {visits.toLocaleString()}</span>}*/}
                {/*{favorites !== undefined && <span className="text-xs">즐겨찾기 {favorites.toLocaleString()}</span>}*/}
                {/*{capacity !== undefined && <span className="text-xs">정원 {capacity}</span>}*/}
            </CardFooter>


        </Card>
    );
}


export function WorldCardSkeleton() {
    return (
        <Card className="overflow-hidden pt-0">
            <AspectRatio ratio={4 / 3}>
                <CardHeader className="p-0 relative h-full">
                    <Skeleton className="absolute inset-0 w-full h-full rounded-none"/>
                </CardHeader>
            </AspectRatio>
            <CardContent className="p-4 space-y-2">
                <Skeleton className="h-5 w-3/5"/>
                <Skeleton className="h-4 w-2/5"/>
            </CardContent>
            <CardFooter className="p-4 pt-0 flex gap-3">
                <Skeleton className="h-3 w-16"/>
                <Skeleton className="h-3 w-20"/>
                <Skeleton className="h-3 w-20"/>
            </CardFooter>
        </Card>
    );
}

function replaceExtension(filename: string, newExt: string): string {
    // newExt 앞에 "."이 없으면 붙여줌
    if (!newExt.startsWith(".")) {
        newExt = "." + newExt;
    }

    const idx = filename.lastIndexOf(".");
    if (idx === -1) {
        // 확장자가 없는 경우 그냥 덧붙임
        return filename + newExt;
    }
    return filename.substring(0, idx) + newExt;
}