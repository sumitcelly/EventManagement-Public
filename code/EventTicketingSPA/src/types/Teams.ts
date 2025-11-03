export type TeamMember = {
  userId:number,
  permissions: string[],
  email: string,
  status:string,
  name:string,
  orgMemberId: number;
}
