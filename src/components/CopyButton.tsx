"use client";

import {useEffect, useRef, useState} from "react";
import {
    Tooltip,
    TooltipContent,
    TooltipProvider,
    TooltipTrigger
} from "@/components/ui/tooltip";
import {CopyIcon} from "lucide-react";

type Props = { text: string; durationMs?: number };

export default function CopyButton({text, durationMs = 700}: Props) {
    const [open, setOpen] = useState(false);
    const tRef = useRef<number | null>(null);

    // 빠른 연속 클릭 시 타이머 리셋
    const showTip = () => {
        setOpen(true);
        if (tRef.current) window.clearTimeout(tRef.current);
        tRef.current = window.setTimeout(() => setOpen(false), durationMs);
    };

    // 언마운트 시 타이머 정리
    useEffect(() => {
        return () => {
            if (tRef.current) window.clearTimeout(tRef.current);
        };
    }, []);

    const copy = async () => {
        try {
            if (navigator.clipboard?.writeText) {
                await navigator.clipboard.writeText(text);
            } else {
                fallbackCopy(text);
            }
            showTip();
        } catch {
            // 실패 시에도 최소한 시각적 피드백
            showTip();
        }
    };

    return (

        <TooltipProvider>
            <Tooltip open={open} delayDuration={0}>
                <TooltipTrigger asChild>
                    <CopyIcon className={"hover:cursor-pointer"} onClick={copy}/>
                </TooltipTrigger>
                <TooltipContent side="top" align="center" sideOffset={8}>
                    복사했어!
                </TooltipContent>
            </Tooltip>
        </TooltipProvider>
    );
}

// 구형 브라우저 폴백 (최소 구현)
function fallbackCopy(text: string) {
    const ta = document.createElement("textarea");
    ta.value = text;
    ta.style.position = "fixed";
    ta.style.opacity = "0";
    document.body.appendChild(ta);
    ta.focus();
    ta.select();
    try {
        document.execCommand("copy");
    } finally {
        document.body.removeChild(ta);
    }
}