"use client";

import {Card, CardContent, CardFooter, CardHeader} from "@/components/ui/card";
import {AspectRatio} from "@/components/ui/aspect-ratio";
import {Skeleton} from "@/components/ui/skeleton";
import {useState} from "react";
import Dict = NodeJS.Dict;
import {Separator} from "@/components/ui/separator";
import Image from "next/image";
import {Label, PolarRadiusAxis, RadialBar, RadialBarChart} from "recharts"
import {ChartConfig, ChartContainer, ChartTooltip, ChartTooltipContent,} from "@/components/ui/chart"
import {HoverCard, HoverCardContent, HoverCardTrigger} from "@/components/ui/hover-card";
import {Badge} from "@/components/ui/badge";
import Link from "next/link";

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

    tags = tags.filter((t) => t.startsWith('author_tag_') || t.startsWith('category_tag_'));
    const hasImageList = Array.isArray(imageList) && imageList.length > 0;

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


            <CardContent className="flex flex-col justify-start items-center w-full grow gap-0 px-4.5">
                {/* 월드 제목 */}
                <div
                    className="text-lg font-bold text-center my-3 p-0 min-h-[52px] flex flex-col items-center w-full grow-0 justify-center leading-6.5">
                    {worldName}
                </div>

                {/* 월드 제작자 */}
                <div className="text-sm text-center mb-2 text-muted-foreground grow-0">
                    {authorName}
                </div>
                <Separator className="m-0"/>

                {/* 월드 기본 정보 */}
                {makeVisitorChart(visits, favorites, "mx-auto w-full h-[90px] my-4")}
                {/* 월드 정원*/}
                <div
                    className="flex flex-col text-sm text-muted-foreground grow-0 justify-center items-center mt-[0.25rem]">
                    <div className="text-xs">최대 인원</div>
                    <div className="text-shadow-xs">{capacity}</div>
                </div>
                <Separator className="m-0"/>

                {/* 월드 Description */}
                <div className="text-sm text-secondary-foreground grow-0 mt-4">
                    뉴비 추천 월드입니다. 시간적, 물리적 제약을 초월하는 VR의 특장점을 제대로 겪어볼 수 있습니다. 방구석에서 실제 일본의 동굴을 체험할 수 있다고?! 정말 아름다운 일이 아닐 수
                    없습니다.
                </div>
                
                {/* 월드 태그 */}
                {tags?.length > 0 && (
                    <div className="flex flex-wrap flex-row justify-center gap-1 mt-4">
                        {tags.map((t) => (
                            <Badge key={t}>
                                {t.replace(/^author_tag_/, '')}
                            </Badge>
                        ))}
                    </div>
                )}

            </CardContent>


            <CardFooter
                className="flex flex-col justify-end items-center w-full grow gap-0 px-4.5 mb-[0.25rem] mt-[0.25rem]">


                {/* 썸네일 리스트: 최대 6개, 초과 시 6번째 위에 반투명 오버레이 +N */}
                {hasImageList && <Separator className="m-0 mt-[0.25rem]"/>}
                {hasImageList && (
                    <div className="mt-[0.25rem] w-full">
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

            </CardFooter>


        </Card>
    );
}

function makeVisitorChart(visits: number, favorites: number, className: string | undefined) {
    const chartData = [{visitors: visits, favorites: favorites}]
    const chartConfig = {
        visitors: {
            label: "방문자 수",
            color: "var(--chart-2)",
        },
        favorites: {
            label: "즐겨찾기",
            color: "var(--chart-1)",
        },
    } satisfies ChartConfig

    return (
        <ChartContainer
            config={chartConfig}
            className={className}
        >
            <RadialBarChart
                data={chartData}
                startAngle={180}
                endAngle={0}
                innerRadius={80}
                outerRadius={130}
                className="p-0 m-0"
                cy={90}
            >
                <ChartTooltip
                    cursor={false}
                    content={<ChartTooltipContent/>}
                />
                <PolarRadiusAxis tick={false} axisLine={false} tickLine={false}>
                    <Label
                        content={({viewBox}) => {
                            if (viewBox && "cx" in viewBox && "cy" in viewBox) {
                                return (
                                    <text x={viewBox.cx} y={viewBox.cy} textAnchor="middle">
                                        <HoverCard openDelay={10} closeDelay={100}>
                                            <HoverCardTrigger>
                                                <tspan
                                                    x={viewBox.cx}
                                                    y={(viewBox.cy || 0) - 42 - 2}
                                                    className="fill-foreground text-sm">
                                                    ⭐{(favorites / visits * 100).toFixed(2)}%{"　"}
                                                </tspan>
                                            </HoverCardTrigger>
                                            <HoverCardContent className="text-center bg-muted text-xs p-3 py-1.5 w-full"
                                                              align="center" side="top">
                                                방문자 즐겨찾기 비율
                                                <p/>
                                                {(favorites / visits * 100).toFixed(2)}%
                                                <p/>
                                                {favorites.toLocaleString()}회
                                            </HoverCardContent>
                                        </HoverCard>
                                        <tspan
                                            x={viewBox.cx}
                                            y={(viewBox.cy || 0) - 16 - 2}
                                            className="fill-foreground text-2xl font-bold">
                                            {visits.toLocaleString()}
                                        </tspan>
                                        <tspan
                                            x={viewBox.cx}
                                            y={(viewBox.cy || 0) - 1}
                                            className="fill-muted-foreground"
                                        >
                                            Visitors
                                        </tspan>
                                    </text>
                                )
                            }
                        }}
                    />
                </PolarRadiusAxis>
                <RadialBar
                    dataKey="favorites"
                    fill="var(--color-favorites)"
                    cornerRadius={1.5}
                    stackId={1}
                />
                <RadialBar
                    dataKey="visitors"
                    cornerRadius={1.5}
                    stackId={1}
                    fill="var(--color-visitors)"
                    opacity={0.5}
                />


            </RadialBarChart>
        </ChartContainer>
    )
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