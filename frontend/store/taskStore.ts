import { create } from "zustand";

export type TaskFilters = {
  status?: string;
  assignedTo?: string;
  search?: string;
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDescending?: boolean;
};

type TaskStoreState = {
  filters: TaskFilters;
  setFilters: (next: Partial<TaskFilters>) => void;
  resetFilters: () => void;
};

const initialFilters: TaskFilters = {
  status: undefined,
  assignedTo: undefined,
  search: "",
  page: 1,
  pageSize: 10,
  sortBy: "createdAt",
  sortDescending: true,
};

export const useTaskStore = create<TaskStoreState>((set) => ({
  filters: initialFilters,
  setFilters: (next) => {
    set((state) => ({
      filters: {
        ...state.filters,
        ...next,
      },
    }));
  },
  resetFilters: () => {
    set({ filters: initialFilters });
  },
}));