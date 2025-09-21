"use client";

import {Card, CardContent, CardFooter, CardHeader} from "@/components/ui/card";
import {AspectRatio} from "@/components/ui/aspect-ratio";
import {Skeleton} from "@/components/ui/skeleton";
import {useState} from "react";
import Dict = NodeJS.Dict;
import {Separator} from "@/components/ui/separator";

export type WorldCardProps = {
    id: string;
    name: string;
    authorId: string;
    authorName: string;
    imageUrl: string;
    capacity: number;
    visits: number;
    favorites: number;
    heat: number;
    popularity: number;
    tags: string[];
    images: Dict<string>[];
};

export default function WorldCard({
                                      id,
                                      name,
                                      authorId,
                                      authorName,
                                      imageUrl,
                                      capacity,
                                      visits,
                                      favorites,
                                      heat,
                                      popularity,
                                      tags,
                                      images,
                                  }: WorldCardProps) {
    const [loaded, setLoaded] = useState(false);
    const [error, setError] = useState(false);

    return (
        <Card className="overflow-hidden py-0 justify-between items-center gap-0 ">
            <AspectRatio ratio={4 / 3}>
                <CardHeader className="p-0 relative h-full">
                    {!loaded && !error && (
                        <Skeleton className="absolute inset-0 w-full h-full rounded-none"/>
                    )}
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                        src={imageUrl}
                        alt={name}
                        className={`absolute inset-0 w-full h-full object-cover bg-gray-100 transition-opacity duration-300 ${loaded ? "opacity-100" : "opacity-0"}`}
                        loading="lazy"
                        onLoad={() => setLoaded(true)}
                        onError={() => {
                            setError(true);
                            setLoaded(false);
                        }}
                    />
                    {error && (
                        <div
                            className="absolute inset-0 flex items-center justify-center text-sm text-gray-500 bg-gray-100">
                            이미지 로드 실패
                        </div>
                    )}
                </CardHeader>
            </AspectRatio>
            <CardContent className="flex flex-col justify-between items-center w-full">
                <div className="text-lg font-bold text-center m-5 p-0 mb-0 min-h-[56px] flex flex-col items-center block w-full">{name}</div>
                <div className="text-sm text-center mt-2 text-gray-500">by {authorName}</div>
                <Separator className="m-5"/>
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
            </CardContent>
            <CardFooter className="p-4 mt-4 text-xs text-gray-600 flex gap-3">

                {visits !== undefined && <span>방문 {visits.toLocaleString()}</span>}
                {favorites !== undefined && <span>즐겨찾기 {favorites.toLocaleString()}</span>}
                {capacity !== undefined && <span>정원 {capacity}</span>}
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