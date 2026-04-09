import { SkeletonBlock, SkeletonLines } from "@/components/feedback/Skeleton";

export default function Loading() {
  return (
    <div className="space-y-6 py-4">
      <SkeletonBlock className="h-36 w-full rounded-2xl" />
      <div className="grid gap-6 lg:grid-cols-2">
        <SkeletonBlock className="h-64 rounded-2xl" />
        <SkeletonLines className="rounded-2xl" lines={6} />
      </div>
    </div>
  );
}