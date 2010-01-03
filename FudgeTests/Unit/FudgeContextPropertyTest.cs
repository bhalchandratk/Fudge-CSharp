﻿/**
 * Copyright (C) 2009 - 2009 by OpenGamma Inc. and other contributors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 *     
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Fudge.Tests.Unit
{
    public class FudgeContextPropertyTest
    {
        [Fact]
        public void TypeValidation()
        {
            var prop = new FudgeContextProperty("test", typeof(string));
            Assert.True(prop.IsValidValue("fred"));
            Assert.False(prop.IsValidValue(17));
        }

        [Fact]
        public void InheritedTypes()
        {
            var prop = new FudgeContextProperty("test", typeof(object));
            Assert.True(prop.IsValidValue(new object()));
            Assert.True(prop.IsValidValue(new FudgeContextPropertyTest()));
        }
    }
}
