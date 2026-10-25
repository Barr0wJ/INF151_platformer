using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D), typeof(LineRenderer))]
public class ZiplineHang : Zipline
{
    protected override bool RideOnUpperSide => false;
}