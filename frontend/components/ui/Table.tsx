import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

export type TableColumn<T> = {
  key: keyof T | string;
  header: string;
  className?: string;
  render?: (row: T) => ReactNode;
};

type Props<T> = {
  columns: Array<TableColumn<T>>;
  rows: T[];
  rowKey: (row: T) => string;
  emptyMessage?: string;
};

export function Table<T>({ columns, rows, rowKey, emptyMessage = "No rows to display." }: Props<T>) {
  return (
    <div className="overflow-hidden rounded-xl border border-border bg-card">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[540px] border-collapse text-left text-sm">
          <thead className="bg-muted/50">
            <tr>
              {columns.map((column) => (
                <th
                  key={String(column.key)}
                  className={cn(
                    "border-b border-border px-4 py-3 text-xs font-semibold uppercase tracking-wide text-muted-foreground",
                    column.className,
                  )}
                >
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>

          <tbody>
            {rows.length === 0 ? (
              <tr>
                <td
                  colSpan={columns.length}
                  className="px-4 py-8 text-center text-sm text-muted-foreground"
                >
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              rows.map((row, index) => (
                <tr
                  key={rowKey(row)}
                  className={cn(
                    "border-b border-border/70 last:border-0",
                    index % 2 === 1 ? "bg-background/45" : "bg-transparent",
                  )}
                >
                  {columns.map((column) => {
                    const rawValue = (row as Record<string, unknown>)[String(column.key)];

                    return (
                      <td key={String(column.key)} className={cn("px-4 py-3 text-sm", column.className)}>
                        {column.render ? column.render(row) : String(rawValue ?? "-")}
                      </td>
                    );
                  })}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}