import { useQuery } from "react-query";
import axiosClient from "../api/axiosClient";


const fetchCompanyDetails = async () => {

  const res = await axiosClient.get(`/payment/companydetails`);
  console.log('Company  details', res?.data);
  return res.data;
};

export function useCompanyDetails() {
  return useQuery(
    ["company"],
    () => fetchCompanyDetails(),
    {
      staleTime: 1000 * 60 * 60,
      cacheTime: 1000 * 60 * 60,
      refetchOnWindowFocus: false,
    }
  );
}


