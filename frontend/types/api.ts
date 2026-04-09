type Primitive = string | number | boolean;

export type QueryParams = Record<string, Primitive | null | undefined>;

export type ApiResponse<T> = {
  success: boolean;
  data: T;
  message: string;
  errors?: string[];
};

export type PaginatedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  totalCount?: number;
};