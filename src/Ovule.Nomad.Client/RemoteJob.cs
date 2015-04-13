﻿/*
Copyright (c) 2015 Tony Di Nucci (tonydinucci[at]gmail[dot]com)
 
This file is part of Nomad.

Nomad is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Nomad is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Nomad.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Linq.Expressions;

namespace Ovule.Nomad.Client
{
  public class RemoteJob
  {
    public Expression<Action> Job;

    public RemoteJob(Expression<Action> job)
    {
      this.ThrowIfArgumentIsNull(() => job);
      Job = job;
    }
  }
}
