import { QueryClient } from "react-query";

export const queryClient = new QueryClient();

export const clearAuthQueryCache = () => {
  queryClient.clear();
};
