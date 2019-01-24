﻿// <copyright file="VideoEncoderTests.cs" company="Techyian">
// Copyright (c) Ian Auty. All rights reserved.
// Licensed under the MIT License. Please see LICENSE.txt for License info.
// </copyright>

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MMALSharp.Components;
using MMALSharp.Config;
using MMALSharp.Handlers;
using MMALSharp.Native;
using MMALSharp.Ports;
using Xunit;

namespace MMALSharp.Tests
{
    public class VideoEncoderTests : TestBase
    {
        #region Configuration tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [MMALTestsAttribute]
        public void SetThenGetVideoStabilisation(bool vstab)
        {
            MMALCameraConfig.VideoStabilisation = vstab;
            Fixture.MMALCamera.ConfigureCameraSettings();
            Assert.True(Fixture.MMALCamera.Camera.GetVideoStabilisation() == vstab);
        }

        #endregion

        [Theory]
        [MemberData("Data", MemberType = typeof(VideoData))]
        public async Task TakeVideo(string extension, MMALEncoding encodingType, MMALEncoding pixelFormat)
        {
            TestHelper.BeginTest("TakeVideo", encodingType.EncodingName, pixelFormat.EncodingName);
            TestHelper.SetConfigurationDefaults();
            TestHelper.CleanDirectory("/home/pi/videos/tests");
                
            using (var vidCaptureHandler = new VideoStreamCaptureHandler("/home/pi/videos/tests", extension))
            using (var preview = new MMALVideoRenderer())
            using (var vidEncoder = new MMALVideoEncoder(vidCaptureHandler))
            {
                Fixture.MMALCamera.ConfigureCameraSettings();

                var portConfig = new MMALPortConfig(encodingType, pixelFormat, 25, 10, 25000000, null);

                vidEncoder.ConfigureOutputPort(portConfig);

                // Create our component pipeline.         
                Fixture.MMALCamera.Camera.VideoPort
                    .ConnectTo(vidEncoder);
                Fixture.MMALCamera.Camera.PreviewPort
                    .ConnectTo(preview);
                    
                // Camera warm up time
                await Task.Delay(2000);

                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                // Record video for 20 seconds
                await Fixture.MMALCamera.ProcessAsync(Fixture.MMALCamera.Camera.VideoPort, cts.Token);

                Fixture.CheckAndAssertFilepath(vidCaptureHandler.GetFilepath());
            }
        }

        [Fact]
        public async Task TakeVideoSplit()
        {
            TestHelper.BeginTest("TakeVideoSplit");
            TestHelper.SetConfigurationDefaults();
            TestHelper.CleanDirectory("/home/pi/videos/tests/split_test");

            MMALCameraConfig.InlineHeaders = true;
            
            using (var vidCaptureHandler = new VideoStreamCaptureHandler("/home/pi/videos/tests/split_test", "h264"))
            using (var preview = new MMALVideoRenderer())
            using (var vidEncoder = new MMALVideoEncoder(vidCaptureHandler, new Split { Mode = TimelapseMode.Second, Value = 15 }))
            {
                Fixture.MMALCamera.ConfigureCameraSettings();

                var portConfig = new MMALPortConfig(MMALEncoding.H264, MMALEncoding.I420, 25, 10, 25000000, null);

                vidEncoder.ConfigureOutputPort(portConfig);

                // Create our component pipeline.         
                Fixture.MMALCamera.Camera.VideoPort
                    .ConnectTo(vidEncoder);
                Fixture.MMALCamera.Camera.PreviewPort
                    .ConnectTo(preview);

                // Camera warm up time
                await Task.Delay(2000);

                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                // 2 files should be created from this test. 
                await Fixture.MMALCamera.ProcessAsync(Fixture.MMALCamera.Camera.VideoPort, cts.Token);
                    
                Assert.True(Directory.GetFiles("/home/pi/videos/tests/split_test").Length == 2);
            }
        }

        [Fact]
        public async Task ChangeEncoderType()
        {
            TestHelper.BeginTest("Video - ChangeEncoderType");
            TestHelper.SetConfigurationDefaults();
            TestHelper.CleanDirectory("/home/pi/videos/tests");
                
            using (var vidCaptureHandler = new VideoStreamCaptureHandler("/home/pi/videos/tests", "h264"))
            using (var preview = new MMALVideoRenderer())
            using (var vidEncoder = new MMALVideoEncoder(vidCaptureHandler))
            {
                Fixture.MMALCamera.ConfigureCameraSettings();

                var portConfig = new MMALPortConfig(MMALEncoding.MJPEG, MMALEncoding.I420, 25, 10, 25000000, null);

                vidEncoder.ConfigureOutputPort(portConfig);

                // Create our component pipeline.         
                Fixture.MMALCamera.Camera.VideoPort
                    .ConnectTo(vidEncoder);
                Fixture.MMALCamera.Camera.PreviewPort
                    .ConnectTo(preview);

                // Camera warm up time
                await Task.Delay(2000);

                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

                // Record video for 20 seconds
                await Fixture.MMALCamera.ProcessAsync(Fixture.MMALCamera.Camera.VideoPort, cts.Token);

                Fixture.CheckAndAssertFilepath(vidCaptureHandler.GetFilepath());
            }
                
            using (var vidCaptureHandler = new VideoStreamCaptureHandler("/home/pi/videos/tests", "mjpeg"))
            using (var preview = new MMALVideoRenderer())
            using (var vidEncoder = new MMALVideoEncoder(vidCaptureHandler))
            {
                Fixture.MMALCamera.ConfigureCameraSettings();

                var portConfig = new MMALPortConfig(MMALEncoding.MJPEG, MMALEncoding.I420, 25, 10, 25000000, null);

                vidEncoder.ConfigureOutputPort(portConfig);

                // Create our component pipeline.         
                Fixture.MMALCamera.Camera.VideoPort
                    .ConnectTo(vidEncoder);
                Fixture.MMALCamera.Camera.PreviewPort
                    .ConnectTo(preview);

                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

                // Record video for 20 seconds
                await Fixture.MMALCamera.ProcessAsync(Fixture.MMALCamera.Camera.VideoPort, cts.Token);

                Fixture.CheckAndAssertFilepath(vidCaptureHandler.GetFilepath());
            }
        }

        [Fact]
        public async Task VideoSplitterComponent()
        {
            TestHelper.BeginTest("VideoSplitterComponent");
            TestHelper.SetConfigurationDefaults();
            TestHelper.CleanDirectory("/home/pi/videos/tests");

            using (var handler = new VideoStreamCaptureHandler("/home/pi/videos/tests", "h264"))
            using (var handler2 = new VideoStreamCaptureHandler("/home/pi/videos/tests", "h264"))
            using (var handler3 = new VideoStreamCaptureHandler("/home/pi/videos/tests", "h264"))
            using (var handler4 = new VideoStreamCaptureHandler("/home/pi/videos/tests", "h264"))
            using (var splitter = new MMALSplitterComponent(null))
            using (var vidEncoder = new MMALVideoEncoder(handler))
            using (var vidEncoder2 = new MMALVideoEncoder(handler2))
            using (var vidEncoder3 = new MMALVideoEncoder(handler3))
            using (var vidEncoder4 = new MMALVideoEncoder(handler4))
            using (var renderer = new MMALNullSinkComponent())
            {
                Fixture.MMALCamera.ConfigureCameraSettings();

                var splitterPortConfig = new MMALPortConfig(MMALEncoding.OPAQUE, MMALEncoding.I420, 10, 0, 13000000, null);
                var portConfig1 = new MMALPortConfig(MMALEncoding.H264, MMALEncoding.I420, 10, 10, 13000000, DateTime.Now.AddSeconds(20));
                var portConfig2 = new MMALPortConfig(MMALEncoding.H264, MMALEncoding.I420, 10, 20, 13000000, DateTime.Now.AddSeconds(15));
                var portConfig3 = new MMALPortConfig(MMALEncoding.H264, MMALEncoding.I420, 25, 30, 13000000, DateTime.Now.AddSeconds(10));
                var portConfig4 = new MMALPortConfig(MMALEncoding.H264, MMALEncoding.I420, 25, 40, 13000000, DateTime.Now.AddSeconds(10));

                // Create our component pipeline.         
                splitter.ConfigureInputPort(MMALEncoding.OPAQUE, MMALEncoding.I420, Fixture.MMALCamera.Camera.VideoPort);
                splitter.ConfigureOutputPort(0, splitterPortConfig);
                splitter.ConfigureOutputPort(1, splitterPortConfig);
                splitter.ConfigureOutputPort(2, splitterPortConfig);
                splitter.ConfigureOutputPort(3, splitterPortConfig);

                vidEncoder.ConfigureInputPort(MMALEncoding.OPAQUE, MMALEncoding.I420, splitter.Outputs[0]);
                vidEncoder.ConfigureOutputPort(0, portConfig1);

                vidEncoder2.ConfigureInputPort(MMALEncoding.OPAQUE, MMALEncoding.I420, splitter.Outputs[1]);
                vidEncoder2.ConfigureOutputPort(0, portConfig2);
                vidEncoder3.ConfigureInputPort(MMALEncoding.OPAQUE, MMALEncoding.I420, splitter.Outputs[2]);
                vidEncoder3.ConfigureOutputPort(0, portConfig3);

                vidEncoder4.ConfigureInputPort(MMALEncoding.OPAQUE, MMALEncoding.I420, splitter.Outputs[3]);
                vidEncoder4.ConfigureOutputPort(0, portConfig4);

                Fixture.MMALCamera.Camera.VideoPort.ConnectTo(splitter);

                splitter.Outputs[0].ConnectTo(vidEncoder);
                splitter.Outputs[1].ConnectTo(vidEncoder2);
                splitter.Outputs[2].ConnectTo(vidEncoder3);
                splitter.Outputs[3].ConnectTo(vidEncoder4);

                Fixture.MMALCamera.Camera.PreviewPort.ConnectTo(renderer);

                // Camera warm up time
                await Task.Delay(2000);

                await Fixture.MMALCamera.ProcessAsync(Fixture.MMALCamera.Camera.VideoPort);

                Fixture.CheckAndAssertFilepath(handler.GetFilepath());
                Fixture.CheckAndAssertFilepath(handler2.GetFilepath());
                Fixture.CheckAndAssertFilepath(handler3.GetFilepath());
                Fixture.CheckAndAssertFilepath(handler4.GetFilepath());
            }
        }
        
        [Fact]
        public void ChangeColorSpace()
        {
            TestHelper.BeginTest("ChangeColorSpace");
            TestHelper.SetConfigurationDefaults();
            TestHelper.CleanDirectory("/home/pi/videos/tests");

            MMALCameraConfig.VideoColorSpace = MMALEncoding.MMAL_COLOR_SPACE_ITUR_BT601;
            
            using (var handler = new VideoStreamCaptureHandler("/home/pi/videos/tests", "h264"))
            using (var handler2 = new VideoStreamCaptureHandler("/home/pi/video/tests", "h264"))
            using (var handler3 = new VideoStreamCaptureHandler("/home/pi/video/tests", "h264"))
            using (var handler4 = new VideoStreamCaptureHandler("/home/pi/video/tests", "h264"))
            using (var splitter = new MMALSplitterComponent(null))
            using (var vidEncoder = new MMALVideoEncoder(handler))
            using (var vidEncoder2 = new MMALVideoEncoder(handler2))
            using (var vidEncoder3 = new MMALVideoEncoder(handler3))
            using (var vidEncoder4 = new MMALVideoEncoder(handler4))
            using (var renderer = new MMALVideoRenderer())
            {
                Fixture.MMALCamera.ConfigureCameraSettings();

                var splitterPortConfig = new MMALPortConfig(MMALEncoding.OPAQUE, MMALEncoding.I420, 10, 0, 13000000, null);
                var portConfig1 = new MMALPortConfig(MMALEncoding.H264, MMALEncoding.I420, 10, 10, 13000000, DateTime.Now.AddSeconds(20));
                var portConfig2 = new MMALPortConfig(MMALEncoding.H264, MMALEncoding.I420, 10, 20, 13000000, DateTime.Now.AddSeconds(15));
                var portConfig3 = new MMALPortConfig(MMALEncoding.H264, MMALEncoding.I420, 25, 30, 13000000, DateTime.Now.AddSeconds(10));
                var portConfig4 = new MMALPortConfig(MMALEncoding.H264, MMALEncoding.I420, 25, 40, 13000000, DateTime.Now.AddSeconds(10));

                // Create our component pipeline.         
                splitter.ConfigureInputPort(MMALEncoding.OPAQUE, MMALEncoding.I420, Fixture.MMALCamera.Camera.VideoPort);
                splitter.ConfigureOutputPort(0, splitterPortConfig);
                splitter.ConfigureOutputPort(1, splitterPortConfig);
                splitter.ConfigureOutputPort(2, splitterPortConfig);
                splitter.ConfigureOutputPort(3, splitterPortConfig);

                vidEncoder.ConfigureInputPort(MMALEncoding.OPAQUE, MMALEncoding.I420, splitter.Outputs[0]);
                vidEncoder.ConfigureOutputPort(0, portConfig1);

                vidEncoder2.ConfigureInputPort(MMALEncoding.OPAQUE, MMALEncoding.I420, splitter.Outputs[1]);
                vidEncoder2.ConfigureOutputPort(0, portConfig2);
                vidEncoder3.ConfigureInputPort(MMALEncoding.OPAQUE, MMALEncoding.I420, splitter.Outputs[2]);
                vidEncoder3.ConfigureOutputPort(0, portConfig3);

                vidEncoder4.ConfigureInputPort(MMALEncoding.OPAQUE, MMALEncoding.I420, splitter.Outputs[3]);
                vidEncoder4.ConfigureOutputPort(0, portConfig4);

                Fixture.MMALCamera.Camera.VideoPort.ConnectTo(splitter);

                splitter.Outputs[0].ConnectTo(vidEncoder);
                splitter.Outputs[1].ConnectTo(vidEncoder2);
                splitter.Outputs[2].ConnectTo(vidEncoder3);
                splitter.Outputs[3].ConnectTo(vidEncoder4);

                Fixture.MMALCamera.Camera.PreviewPort.ConnectTo(renderer);

                // Assert that all output ports have the same video color space.
                Assert.True(Fixture.MMALCamera.Camera.VideoPort.VideoColorSpace.EncodingVal == MMALEncoding.MMAL_COLOR_SPACE_ITUR_BT601.EncodingVal);
                
                Assert.True(splitter.Outputs[0].VideoColorSpace.EncodingVal == MMALEncoding.MMAL_COLOR_SPACE_ITUR_BT601.EncodingVal);
                Assert.True(splitter.Outputs[1].VideoColorSpace.EncodingVal == MMALEncoding.MMAL_COLOR_SPACE_ITUR_BT601.EncodingVal);
                Assert.True(splitter.Outputs[2].VideoColorSpace.EncodingVal == MMALEncoding.MMAL_COLOR_SPACE_ITUR_BT601.EncodingVal);
                Assert.True(splitter.Outputs[3].VideoColorSpace.EncodingVal == MMALEncoding.MMAL_COLOR_SPACE_ITUR_BT601.EncodingVal);
                
                Assert.True(vidEncoder.Outputs[0].VideoColorSpace.EncodingVal == MMALEncoding.MMAL_COLOR_SPACE_ITUR_BT601.EncodingVal);
                Assert.True(vidEncoder2.Outputs[0].VideoColorSpace.EncodingVal == MMALEncoding.MMAL_COLOR_SPACE_ITUR_BT601.EncodingVal);
                Assert.True(vidEncoder3.Outputs[0].VideoColorSpace.EncodingVal == MMALEncoding.MMAL_COLOR_SPACE_ITUR_BT601.EncodingVal);
                Assert.True(vidEncoder4.Outputs[0].VideoColorSpace.EncodingVal == MMALEncoding.MMAL_COLOR_SPACE_ITUR_BT601.EncodingVal);
            }
        }
    }
}
