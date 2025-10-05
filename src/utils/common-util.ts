export function replaceExtension(filename: string, newExt: string): string {
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

export function notNull<T>(item: T | null): item is T {
    return item !== null;
}