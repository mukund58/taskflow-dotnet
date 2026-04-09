import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition",
  {
    variants: {
      variant: {
        default: "border-transparent bg-primary text-primary-foreground hover:bg-primary/85",
        secondary: "border-transparent bg-secondary text-secondary-foreground hover:bg-secondary/85",
        destructive: "border-transparent bg-destructive text-destructive-foreground hover:bg-destructive/90",
        outline: "border-border text-foreground",
        neutral: "border-border bg-card text-card-foreground",
        success: "border border-[var(--color-success)]/35 bg-[var(--color-success)]/12 text-[var(--color-success)]",
        warning: "border border-[var(--color-warning)]/35 bg-[var(--color-warning)]/12 text-[var(--color-warning)]",
        danger: "border border-[var(--color-danger)]/35 bg-[var(--color-danger)]/12 text-[var(--color-danger)]",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  },
);

export type BadgeVariant = NonNullable<VariantProps<typeof badgeVariants>["variant"]>;

type Props = React.ComponentProps<"span"> & VariantProps<typeof badgeVariants>;

export function Badge({ className, variant, children, ...props }: Props) {
  return (
    <span
      data-slot="badge"
      className={cn(badgeVariants({ variant }), className)}
      {...props}
    >
      {children}
    </span>
  );
}

export { badgeVariants };