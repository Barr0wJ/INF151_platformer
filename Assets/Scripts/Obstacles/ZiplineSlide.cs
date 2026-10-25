using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D), typeof(LineRenderer))]
public class ZiplineSlide : Zipline
{
    protected override bool RideOnUpperSide => true;
}