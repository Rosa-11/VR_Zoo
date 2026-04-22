#ifndef MY_CGINC
#define MY_CGINC

float3 envdiffusecol(float3 upcol,float3 downcol,float3 ralcol,float3 ndirWS)
{
    float3 upmask=max(ndirWS.g,0.0);
    float3 downmask=max(-ndirWS,0.0);
    float3 ralmask=max(1-upmask-downmask,0.0);
    float3 maskcol=upmask*upcol+downmask*downcol+ralmask*ralcol;
   return maskcol;
}

#endif